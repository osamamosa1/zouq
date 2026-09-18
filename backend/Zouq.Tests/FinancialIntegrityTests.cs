using Microsoft.EntityFrameworkCore;
using Zouq.Application.DTOs;
using Zouq.Domain.Enums;
using Zouq.Infrastructure.Services;

namespace Zouq.Tests;

public class FinancialIntegrityTests
{
    [Fact]
    public async Task Order_created_does_not_credit_creator_reward()
    {
        await using var fx = await FinancialTestFixture.CreateAsync();
        var design = await fx.CreateDesignAsync();

        var order = await fx.Orders.CreateFromDesignAsync(fx.Buyer.Id, new CreateOrderRequest(design.Id, 1, null, null));

        Assert.Equal(OrderStatus.Pending.ToString(), order.Status);
        Assert.Equal(0, await fx.Db.CreatorRewards.CountAsync());
        Assert.Equal(0, await fx.Db.LedgerEntries.CountAsync());
        var creator = await fx.Db.Users.AsNoTracking().FirstAsync(u => u.Id == fx.Creator.Id);
        Assert.Equal(0m, creator.Balance);
        var dbOrder = await fx.Db.Orders.AsNoTracking().FirstAsync(o => o.Id == order.Id);
        Assert.Equal(10m, dbOrder.CommissionPercentSnapshot);
        Assert.Null(dbOrder.CreatorRewardAmount);
    }

    [Fact]
    public async Task Delivered_credits_exactly_one_reward()
    {
        await using var fx = await FinancialTestFixture.CreateAsync();
        var design = await fx.CreateDesignAsync();
        var order = await fx.Orders.CreateFromDesignAsync(fx.Buyer.Id, new CreateOrderRequest(design.Id, 1, null, null));

        await fx.AdvanceToAsync(order.Id, OrderStatus.Delivered);

        Assert.Equal(1, await fx.Db.CreatorRewards.CountAsync());
        Assert.Equal(1, await fx.Db.LedgerEntries.CountAsync(l => l.TransactionType == LedgerTransactionType.CreatorReward));
        var creator = await fx.Db.Users.AsNoTracking().FirstAsync(u => u.Id == fx.Creator.Id);
        // Stub price 200 * 10% = 20
        Assert.Equal(20m, creator.Balance);
        var dbOrder = await fx.Db.Orders.AsNoTracking().FirstAsync(o => o.Id == order.Id);
        Assert.Equal(OrderStatus.Delivered, dbOrder.Status);
        Assert.Equal(20m, dbOrder.CreatorRewardAmount);
        var reward = await fx.Db.CreatorRewards.AsNoTracking().SingleAsync();
        Assert.Equal(10m, reward.CommissionPercent);
        Assert.Equal(20m, reward.Amount);
    }

    [Fact]
    public async Task Delivered_twice_does_not_create_second_reward()
    {
        await using var fx = await FinancialTestFixture.CreateAsync();
        var design = await fx.CreateDesignAsync();
        var order = await fx.Orders.CreateFromDesignAsync(fx.Buyer.Id, new CreateOrderRequest(design.Id, 1, null, null));
        await fx.AdvanceToAsync(order.Id, OrderStatus.Delivered);

        await fx.Orders.TransitionStatusAsync(order.Id, OrderStatus.Delivered);
        await fx.Orders.TransitionStatusAsync(order.Id, OrderStatus.Delivered);

        Assert.Equal(1, await fx.Db.CreatorRewards.CountAsync());
        Assert.Equal(1, await fx.Db.LedgerEntries.CountAsync(l => l.TransactionType == LedgerTransactionType.CreatorReward));
        var creator = await fx.Db.Users.AsNoTracking().FirstAsync(u => u.Id == fx.Creator.Id);
        Assert.Equal(20m, creator.Balance);
    }

    [Fact]
    public async Task Concurrent_Delivered_requests_do_not_duplicate_reward()
    {
        await using var fx = await FinancialTestFixture.CreateAsync();
        var design = await fx.CreateDesignAsync();
        var order = await fx.Orders.CreateFromDesignAsync(fx.Buyer.Id, new CreateOrderRequest(design.Id, 1, null, null));
        await fx.AdvanceToAsync(order.Id, OrderStatus.Shipped);

        var t1 = fx.Orders.TransitionStatusAsync(order.Id, OrderStatus.Delivered);
        var t2 = fx.Orders.TransitionStatusAsync(order.Id, OrderStatus.Delivered);
        await Task.WhenAll(t1, t2);

        Assert.Equal(1, await fx.Db.CreatorRewards.CountAsync());
        Assert.Equal(1, await fx.Db.LedgerEntries.CountAsync(l => l.TransactionType == LedgerTransactionType.CreatorReward));
        var creator = await fx.Db.Users.AsNoTracking().FirstAsync(u => u.Id == fx.Creator.Id);
        Assert.Equal(20m, creator.Balance);
    }

    [Fact]
    public async Task Two_orders_same_creator_concurrent_both_credited()
    {
        await using var fx = await FinancialTestFixture.CreateAsync();
        var d1 = await fx.CreateDesignAsync();
        var d2 = await fx.CreateDesignAsync();
        var o1 = await fx.Orders.CreateFromDesignAsync(fx.Buyer.Id, new CreateOrderRequest(d1.Id, 1, null, null));
        var o2 = await fx.Orders.CreateFromDesignAsync(fx.Buyer.Id, new CreateOrderRequest(d2.Id, 1, null, null));
        await fx.AdvanceToAsync(o1.Id, OrderStatus.Shipped);
        await fx.AdvanceToAsync(o2.Id, OrderStatus.Shipped);

        await Task.WhenAll(
            fx.Orders.TransitionStatusAsync(o1.Id, OrderStatus.Delivered),
            fx.Orders.TransitionStatusAsync(o2.Id, OrderStatus.Delivered));

        Assert.Equal(2, await fx.Db.CreatorRewards.CountAsync());
        Assert.Equal(2, await fx.Db.LedgerEntries.CountAsync(l => l.TransactionType == LedgerTransactionType.CreatorReward));
        var creator = await fx.Db.Users.AsNoTracking().FirstAsync(u => u.Id == fx.Creator.Id);
        Assert.Equal(40m, creator.Balance);
    }

    [Fact]
    public async Task Historical_commission_unchanged_after_global_change()
    {
        await using var fx = await FinancialTestFixture.CreateAsync();
        var design = await fx.CreateDesignAsync();
        var order = await fx.Orders.CreateFromDesignAsync(fx.Buyer.Id, new CreateOrderRequest(design.Id, 1, null, null));

        var setting = await fx.Db.CommissionSettings.FirstAsync();
        setting.DesignerCommissionPercent = 15m;
        await fx.Db.SaveChangesAsync();

        await fx.AdvanceToAsync(order.Id, OrderStatus.Delivered);

        var reward = await fx.Db.CreatorRewards.AsNoTracking().SingleAsync();
        Assert.Equal(10m, reward.CommissionPercent);
        Assert.Equal(20m, reward.Amount);
        var dbOrder = await fx.Db.Orders.AsNoTracking().FirstAsync(o => o.Id == order.Id);
        Assert.Equal(10m, dbOrder.CommissionPercentSnapshot);
    }

    [Fact]
    public async Task Cancel_before_Delivered_no_reward()
    {
        await using var fx = await FinancialTestFixture.CreateAsync();
        var design = await fx.CreateDesignAsync();
        var order = await fx.Orders.CreateFromDesignAsync(fx.Buyer.Id, new CreateOrderRequest(design.Id, 1, null, null));

        await fx.Orders.TransitionStatusAsync(order.Id, OrderStatus.Cancelled);

        Assert.Equal(0, await fx.Db.CreatorRewards.CountAsync());
        Assert.Equal(0, await fx.Db.LedgerEntries.CountAsync());
        var creator = await fx.Db.Users.AsNoTracking().FirstAsync(u => u.Id == fx.Creator.Id);
        Assert.Equal(0m, creator.Balance);
    }

    [Fact]
    public async Task Refund_after_reward_creates_reversal_debit()
    {
        await using var fx = await FinancialTestFixture.CreateAsync();
        var design = await fx.CreateDesignAsync();
        var order = await fx.Orders.CreateFromDesignAsync(fx.Buyer.Id, new CreateOrderRequest(design.Id, 1, null, null));
        await fx.AdvanceToAsync(order.Id, OrderStatus.Delivered);

        await fx.Orders.TransitionStatusAsync(order.Id, OrderStatus.Refunded);

        Assert.Equal(1, await fx.Db.CreatorRewards.CountAsync(r => r.Status == CreatorRewardStatus.Reversed));
        Assert.Equal(1, await fx.Db.LedgerEntries.CountAsync(l => l.TransactionType == LedgerTransactionType.CreatorReward));
        Assert.Equal(1, await fx.Db.LedgerEntries.CountAsync(l => l.TransactionType == LedgerTransactionType.CreatorRewardReversal));
        // Original credit never deleted
        Assert.Equal(2, await fx.Db.LedgerEntries.CountAsync());
        var creator = await fx.Db.Users.AsNoTracking().FirstAsync(u => u.Id == fx.Creator.Id);
        Assert.Equal(0m, creator.Balance);
    }

    [Fact]
    public async Task Ledger_and_balance_remain_consistent()
    {
        await using var fx = await FinancialTestFixture.CreateAsync();
        var design = await fx.CreateDesignAsync();
        var order = await fx.Orders.CreateFromDesignAsync(fx.Buyer.Id, new CreateOrderRequest(design.Id, 1, null, null));
        await fx.AdvanceToAsync(order.Id, OrderStatus.Delivered);

        var creator = await fx.Db.Users.AsNoTracking().FirstAsync(u => u.Id == fx.Creator.Id);
        var entries = await fx.Db.LedgerEntries.AsNoTracking()
            .Where(l => l.UserId == creator.Id)
            .ToListAsync();
        var credits = entries.Where(l => l.EntryType == LedgerEntryType.Credit).Sum(l => l.Amount);
        var debits = entries.Where(l => l.EntryType == LedgerEntryType.Debit).Sum(l => l.Amount);
        Assert.Equal(creator.Balance, credits - debits);
    }

    [Fact]
    public async Task Financial_records_cannot_be_deleted_via_SaveChanges()
    {
        await using var fx = await FinancialTestFixture.CreateAsync();
        var design = await fx.CreateDesignAsync();
        var order = await fx.Orders.CreateFromDesignAsync(fx.Buyer.Id, new CreateOrderRequest(design.Id, 1, null, null));
        await fx.AdvanceToAsync(order.Id, OrderStatus.Delivered);

        var ledger = await fx.Db.LedgerEntries.FirstAsync();
        // Restrict relationships + immutability guard prevent deletion
        await Assert.ThrowsAnyAsync<InvalidOperationException>(async () =>
        {
            fx.Db.LedgerEntries.Remove(ledger);
            await fx.Db.SaveChangesAsync();
        });
        Assert.Equal(1, await fx.Db.LedgerEntries.CountAsync());
    }

    [Fact]
    public async Task Cascade_delete_of_order_is_restricted()
    {
        await using var fx = await FinancialTestFixture.CreateAsync();
        var design = await fx.CreateDesignAsync();
        var orderDto = await fx.Orders.CreateFromDesignAsync(fx.Buyer.Id, new CreateOrderRequest(design.Id, 1, null, null));
        await fx.AdvanceToAsync(orderDto.Id, OrderStatus.Delivered);

        var order = await fx.Db.Orders.FirstAsync(o => o.Id == orderDto.Id);
        await Assert.ThrowsAnyAsync<InvalidOperationException>(async () =>
        {
            fx.Db.Orders.Remove(order);
            await fx.Db.SaveChangesAsync();
        });
        Assert.Equal(1, await fx.Db.LedgerEntries.CountAsync());
        Assert.Equal(1, await fx.Db.CreatorRewards.CountAsync());
    }

    [Fact]
    public async Task Admin_balance_adjustment_creates_audited_ledger_entry()
    {
        await using var fx = await FinancialTestFixture.CreateAsync();

        var balance = await fx.Ledger.AdjustBalanceAsync(fx.Creator.Id, 50m, "Goodwill credit", fx.Admin.Id);

        Assert.Equal(50m, balance);
        var entry = await fx.Db.LedgerEntries.AsNoTracking().SingleAsync();
        Assert.Equal(LedgerTransactionType.AdminAdjustment, entry.TransactionType);
        Assert.Equal(LedgerEntryType.Credit, entry.EntryType);
        Assert.Equal(50m, entry.Amount);
        Assert.Equal(fx.Admin.Id, entry.CreatedByAdminId);
        Assert.Equal("Goodwill credit", entry.Reason);
        Assert.StartsWith("admin-adjustment:", entry.Reference);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fx.Ledger.AdjustBalanceAsync(fx.Creator.Id, 10m, "  ", fx.Admin.Id));
    }

    [Fact]
    public async Task Illegal_status_transition_is_rejected()
    {
        await using var fx = await FinancialTestFixture.CreateAsync();
        var design = await fx.CreateDesignAsync();
        var order = await fx.Orders.CreateFromDesignAsync(fx.Buyer.Id, new CreateOrderRequest(design.Id, 1, null, null));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fx.Orders.TransitionStatusAsync(order.Id, OrderStatus.Delivered));
        Assert.Equal(0, await fx.Db.CreatorRewards.CountAsync());
    }
}
