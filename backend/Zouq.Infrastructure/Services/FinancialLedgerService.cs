using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Zouq.Domain.Entities;
using Zouq.Domain.Enums;
using Zouq.Infrastructure.Data;

namespace Zouq.Infrastructure.Services;

/// <summary>
/// Append-only ledger operations. Balance updates always go through a ledger row.
/// Creator rewards are idempotent via unique (OrderId, CreatorId) and unique Reference.
/// </summary>
public class FinancialLedgerService
{
    private readonly ZouqDbContext _db;
    private readonly ILogger<FinancialLedgerService> _logger;

    public FinancialLedgerService(ZouqDbContext db, ILogger<FinancialLedgerService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public static string CreatorRewardReference(Guid orderId, Guid creatorId) =>
        $"creator-reward:{orderId:N}:{creatorId:N}";

    public static string CreatorRewardReversalReference(Guid orderId, Guid creatorId) =>
        $"creator-reward-reversal:{orderId:N}:{creatorId:N}";

    public async Task CreditCreatorRewardsForDeliveredOrderAsync(Order order, CancellationToken ct = default)
    {
        if (order.CommissionPercentSnapshot <= 0)
            return;

        var eligibleGroups = order.Items
            .Where(i => !i.IsDeleted
                        && i.DesignerId is Guid designerId
                        && designerId != order.BuyerId
                        && i.LineTotal > 0)
            .GroupBy(i => i.DesignerId!.Value)
            .ToList();

        decimal totalCreditedForOrder = 0;

        foreach (var group in eligibleGroups)
        {
            var creatorId = group.Key;
            var reference = CreatorRewardReference(order.Id, creatorId);

            // App-level short-circuit (DB unique is the real guarantee)
            var existing = await _db.CreatorRewards
                .FirstOrDefaultAsync(r => r.OrderId == order.Id && r.CreatorId == creatorId && !r.IsDeleted, ct);
            if (existing != null)
            {
                totalCreditedForOrder += existing.Status == CreatorRewardStatus.Credited ? existing.Amount : 0;
                continue;
            }

            if (await _db.LedgerEntries.AnyAsync(l => l.Reference == reference, ct))
                continue;

            var lineTotal = group.Sum(i => i.LineTotal);
            var amount = Math.Round(lineTotal * (order.CommissionPercentSnapshot / 100m), 2, MidpointRounding.AwayFromZero);
            if (amount <= 0) continue;

            var primaryItem = group.First();
            var creator = await _db.Users.FirstAsync(u => u.Id == creatorId, ct);

            creator.Balance += amount;
            creator.ConcurrencyStamp++;
            var ledger = new LedgerEntry
            {
                UserId = creator.Id,
                EntryType = LedgerEntryType.Credit,
                TransactionType = LedgerTransactionType.CreatorReward,
                Status = LedgerEntryStatus.Posted,
                Amount = amount,
                BalanceAfter = creator.Balance,
                Reason = "Creator reward on order delivered",
                Reference = reference,
                CommissionPercent = order.CommissionPercentSnapshot,
                OrderId = order.Id,
                OrderItemId = primaryItem.Id,
                DesignId = primaryItem.SourceDesignId,
                CreatorId = creatorId
            };
            _db.LedgerEntries.Add(ledger);

            var reward = new CreatorReward
            {
                OrderId = order.Id,
                OrderItemId = primaryItem.Id,
                CreatorId = creatorId,
                DesignId = primaryItem.SourceDesignId,
                CommissionPercent = order.CommissionPercentSnapshot,
                Amount = amount,
                Status = CreatorRewardStatus.Credited,
                CreditLedgerEntry = ledger
            };
            _db.CreatorRewards.Add(reward);
            totalCreditedForOrder += amount;
            _logger.LogInformation(
                "Creator reward credited: Order={OrderId} Creator={CreatorId} Amount={Amount} CommissionPercent={Percent}",
                order.Id, creatorId, amount, order.CommissionPercentSnapshot);
        }

        if (totalCreditedForOrder > 0 || eligibleGroups.Count == 0)
            order.CreatorRewardAmount = totalCreditedForOrder > 0 ? totalCreditedForOrder : order.CreatorRewardAmount ?? 0;

        if (eligibleGroups.Count > 0 && totalCreditedForOrder == 0)
        {
            // All creators already had rewards (idempotent re-entry)
            var sum = await _db.CreatorRewards.AsNoTracking()
                .Where(r => r.OrderId == order.Id && r.Status == CreatorRewardStatus.Credited && !r.IsDeleted)
                .SumAsync(r => (decimal?)r.Amount, ct) ?? 0;
            order.CreatorRewardAmount = sum;
        }
        else if (eligibleGroups.Count == 0)
        {
            order.CreatorRewardAmount ??= 0;
        }
    }

    public async Task ReverseCreatorRewardsForOrderAsync(Order order, string reason, CancellationToken ct = default)
    {
        var rewards = await _db.CreatorRewards
            .Where(r => r.OrderId == order.Id && r.Status == CreatorRewardStatus.Credited && !r.IsDeleted)
            .ToListAsync(ct);

        foreach (var reward in rewards)
        {
            var reversalRef = CreatorRewardReversalReference(order.Id, reward.CreatorId);
            if (await _db.LedgerEntries.AnyAsync(l => l.Reference == reversalRef, ct))
            {
                reward.Status = CreatorRewardStatus.Reversed;
                continue;
            }

            var creator = await _db.Users.FirstAsync(u => u.Id == reward.CreatorId, ct);
            creator.Balance -= reward.Amount;
            creator.ConcurrencyStamp++;

            var ledger = new LedgerEntry
            {
                UserId = creator.Id,
                EntryType = LedgerEntryType.Debit,
                TransactionType = LedgerTransactionType.CreatorRewardReversal,
                Status = LedgerEntryStatus.Posted,
                Amount = reward.Amount,
                BalanceAfter = creator.Balance,
                Reason = reason,
                Reference = reversalRef,
                CommissionPercent = reward.CommissionPercent,
                OrderId = order.Id,
                OrderItemId = reward.OrderItemId,
                DesignId = reward.DesignId,
                CreatorId = reward.CreatorId
            };
            _db.LedgerEntries.Add(ledger);
            reward.Status = CreatorRewardStatus.Reversed;
            reward.ReversalLedgerEntry = ledger;
        }
    }

    /// <summary>
    /// Manual admin wallet adjustment — always creates an audited ledger row.
    /// </summary>
    public async Task<decimal> AdjustBalanceAsync(
        Guid userId,
        decimal signedAmount,
        string reason,
        Guid adminUserId,
        CancellationToken ct = default)
    {
        if (signedAmount == 0)
            throw new InvalidOperationException("Adjustment amount cannot be zero.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Adjustment reason is required.");

        const int maxAttempts = 5;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await using var tx = await _db.Database.BeginTransactionAsync(ct);
                var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, ct)
                    ?? throw new InvalidOperationException("User not found.");

                user.Balance += signedAmount;
                user.ConcurrencyStamp++;
                var entry = new LedgerEntry
                {
                    UserId = user.Id,
                    EntryType = signedAmount >= 0 ? LedgerEntryType.Credit : LedgerEntryType.Debit,
                    TransactionType = LedgerTransactionType.AdminAdjustment,
                    Status = LedgerEntryStatus.Posted,
                    Amount = Math.Abs(signedAmount),
                    BalanceAfter = user.Balance,
                    Reason = reason.Trim(),
                    Reference = $"admin-adjustment:{Guid.NewGuid():N}",
                    CreatedByAdminId = adminUserId
                };
                _db.LedgerEntries.Add(entry);
                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
                _logger.LogInformation(
                    "Admin balance adjustment: User={UserId} Amount={Amount} Admin={AdminId} Reason={Reason}",
                    userId, signedAmount, adminUserId, reason);
                return user.Balance;
            }
            catch (DbUpdateConcurrencyException) when (attempt < maxAttempts)
            {
                foreach (var entry in _db.ChangeTracker.Entries().ToList())
                    entry.State = EntityState.Detached;
            }
        }

        throw new InvalidOperationException("Could not adjust balance due to concurrent updates.");
    }
}
