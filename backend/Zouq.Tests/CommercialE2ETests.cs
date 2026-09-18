using Microsoft.EntityFrameworkCore;
using Zouq.Application.DTOs;
using Zouq.Domain.Enums;
using Zouq.Infrastructure.Services;

namespace Zouq.Tests;

/// <summary>
/// Full commercial journey: A creates → Delivered → Publish → B derives → modifies → orders →
/// Delivered pays A once → C still reuses source → refund compensating debit → no double-pay.
/// </summary>
public class CommercialE2ETests
{
    [Fact]
    public async Task Full_reuse_publish_pay_and_refund_journey()
    {
        await using var fx = await OrderIntegrityFixture.CreateAsync();
        var designs = new DesignAppService(fx.Db, fx.Pricing, fx.Integrity);
        var feed = new FeedAppService(fx.Db);

        // --- User A: create design + order ---
        var designA = await fx.CreateValidDesignAsync();
        Assert.Equal(DesignStatus.Draft, designA.Status);
        Assert.Equal(DesignVisibility.Private, designA.Visibility);

        var orderA = await fx.Orders.CreateFromDesignAsync(
            fx.Creator.Id, new CreateOrderRequest(designA.Id, 1, null, null));
        Assert.Equal(0, await fx.Db.CreatorRewards.CountAsync());
        Assert.DoesNotContain(await feed.ForYouAsync(null), i => i.DesignId == designA.Id);

        await fx.Orders.TransitionStatusAsync(orderA.Id, OrderStatus.Processing);
        await fx.Orders.TransitionStatusAsync(orderA.Id, OrderStatus.InProduction);
        await fx.Orders.TransitionStatusAsync(orderA.Id, OrderStatus.Shipped);
        await fx.Orders.TransitionStatusAsync(orderA.Id, OrderStatus.Delivered);

        var eligible = await fx.Db.Designs.AsNoTracking().FirstAsync(d => d.Id == designA.Id);
        Assert.Equal(DesignStatus.DeliveredEligible, eligible.Status);
        Assert.DoesNotContain(await feed.ForYouAsync(null), i => i.DesignId == designA.Id);

        // --- Publish ---
        await designs.PublishAsync(fx.Creator.Id, designA.Id);
        var published = await fx.Db.Designs.AsNoTracking().FirstAsync(d => d.Id == designA.Id);
        Assert.Equal(DesignStatus.Reusable, published.Status);
        Assert.Equal(DesignVisibility.Public, published.Visibility);
        Assert.Contains(await feed.ForYouAsync(null), i => i.DesignId == designA.Id);

        // --- User B: derive (do not mutate source) ---
        var draftB = await designs.DeriveAsync(fx.Buyer.Id, designA.Id);
        Assert.Equal(fx.Buyer.Id, (await fx.Db.Designs.FirstAsync(d => d.Id == draftB.Id)).OwnerId);
        Assert.Equal(designA.Id, draftB.DerivedFromDesignId);

        var sourceAfterDerive = await fx.Db.Designs.AsNoTracking().FirstAsync(d => d.Id == designA.Id);
        Assert.Equal(DesignStatus.Reusable, sourceAfterDerive.Status);
        Assert.Equal(DesignVisibility.Public, sourceAfterDerive.Visibility);
        Assert.Equal(fx.Creator.Id, sourceAfterDerive.OwnerId);

        // B cannot order A's source directly
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            fx.Orders.CreateFromDesignAsync(fx.Buyer.Id, new CreateOrderRequest(designA.Id, 1, null, null)));

        // B modifies derived draft only
        fx.Db.ChangeTracker.Clear();
        var draftEntity = await fx.Db.Designs.Include(d => d.Elements).FirstAsync(d => d.Id == draftB.Id);
        draftEntity.Title = "B customized";
        var el = draftEntity.Elements.First();
        var sourceNormX = await fx.Db.DesignElements.AsNoTracking()
            .Where(e => e.DesignId == designA.Id).Select(e => e.NormX).FirstAsync();
        el.NormX = 0.15;
        await fx.Db.SaveChangesAsync();
        Assert.Equal(sourceNormX, await fx.Db.DesignElements.AsNoTracking()
            .Where(e => e.DesignId == designA.Id).Select(e => e.NormX).FirstAsync());

        // --- B creates order ---
        var orderB = await fx.Orders.CreateFromDesignAsync(
            fx.Buyer.Id, new CreateOrderRequest(draftB.Id, 1, null, null));
        Assert.Equal(0, await fx.Db.CreatorRewards.CountAsync(r => r.OrderId == orderB.Id));

        var itemB = await fx.Db.OrderItems.AsNoTracking().FirstAsync(i => i.OrderId == orderB.Id);
        Assert.Equal(draftB.Id, itemB.SourceDesignId);
        Assert.Equal(fx.Creator.Id, itemB.DesignerId);
        Assert.Equal(fx.Buyer.Id, (await fx.Db.Orders.AsNoTracking().FirstAsync(o => o.Id == orderB.Id)).BuyerId);

        var detail = await fx.Orders.GetMineAsync(fx.Buyer.Id, orderB.Id);
        Assert.Equal(orderB.Id, detail.Id);
        Assert.Contains(detail.Items, i => i.DesignerId == fx.Creator.Id);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            fx.Orders.GetMineAsync(fx.Creator.Id, orderB.Id));

        var balanceBefore = (await fx.Db.Users.AsNoTracking().FirstAsync(u => u.Id == fx.Creator.Id)).Balance;

        await fx.Orders.TransitionStatusAsync(orderB.Id, OrderStatus.Processing);
        await fx.Orders.TransitionStatusAsync(orderB.Id, OrderStatus.InProduction);
        await fx.Orders.TransitionStatusAsync(orderB.Id, OrderStatus.Shipped);
        await fx.Orders.TransitionStatusAsync(orderB.Id, OrderStatus.Delivered);

        Assert.Equal(1, await fx.Db.CreatorRewards.CountAsync(r => r.CreatorId == fx.Creator.Id && r.OrderId == orderB.Id));
        Assert.Equal(1, await fx.Db.LedgerEntries.CountAsync(l =>
            l.UserId == fx.Creator.Id && l.OrderId == orderB.Id &&
            l.TransactionType == LedgerTransactionType.CreatorReward));

        var balanceAfter = (await fx.Db.Users.AsNoTracking().FirstAsync(u => u.Id == fx.Creator.Id)).Balance;
        Assert.True(balanceAfter > balanceBefore);

        // Repeated Delivered does not double-pay
        await fx.Orders.TransitionStatusAsync(orderB.Id, OrderStatus.Delivered);
        Assert.Equal(1, await fx.Db.CreatorRewards.CountAsync(r => r.OrderId == orderB.Id));

        // --- User C can still derive same source ---
        var userC = new Domain.Entities.User
        {
            Name = "UserC",
            Email = "c-e2e@test.zouq",
            PasswordHash = "x",
            Role = UserRole.Customer
        };
        fx.Db.Users.Add(userC);
        await fx.Db.SaveChangesAsync();

        var draftC = await designs.DeriveAsync(userC.Id, designA.Id);
        Assert.Equal(designA.Id, draftC.DerivedFromDesignId);
        Assert.NotEqual(draftB.Id, draftC.Id);

        var sourceStill = await fx.Db.Designs.AsNoTracking().FirstAsync(d => d.Id == designA.Id);
        Assert.Equal(DesignStatus.Reusable, sourceStill.Status);
        Assert.Equal(DesignVisibility.Public, sourceStill.Visibility);

        // C cannot mutate source (ownership) — SaveAsync requires owner match
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            designs.SaveAsync(userC.Id, new SaveDesignRequest(
                designA.ProductId, null, null, null, null,
                "hack", null, "Draft", "Private", Array.Empty<DesignElementInputDto>()), designA.Id));

        // --- Refund order B: compensating debit, no history deletion ---
        var creditsBeforeRefund = await fx.Db.LedgerEntries.CountAsync(l => l.OrderId == orderB.Id);
        await fx.Orders.TransitionStatusAsync(orderB.Id, OrderStatus.Refunded);
        Assert.True(await fx.Db.LedgerEntries.CountAsync(l => l.OrderId == orderB.Id) > creditsBeforeRefund);
        Assert.Equal(1, await fx.Db.CreatorRewards.CountAsync(r => r.OrderId == orderB.Id)); // reward row kept
        var balanceRefunded = (await fx.Db.Users.AsNoTracking().FirstAsync(u => u.Id == fx.Creator.Id)).Balance;
        Assert.True(balanceRefunded < balanceAfter);
    }
}
