using Microsoft.EntityFrameworkCore;
using Zouq.Application.DTOs;
using Zouq.Domain.Enums;
using Zouq.Infrastructure.Services;

namespace Zouq.Tests;

public class DesignReuseTests
{
    private static async Task<(OrderIntegrityFixture fx, DesignAppService designs, Guid publishedId)> PublishReusableAsync()
    {
        var fx = await OrderIntegrityFixture.CreateAsync();
        var designs = new DesignAppService(fx.Db, fx.Pricing, fx.Integrity);
        var design = await fx.CreateValidDesignAsync();
        var order = await fx.Orders.CreateFromDesignAsync(fx.Creator.Id, new CreateOrderRequest(design.Id, 1, null, null));
        await fx.Orders.TransitionStatusAsync(order.Id, OrderStatus.Processing);
        await fx.Orders.TransitionStatusAsync(order.Id, OrderStatus.InProduction);
        await fx.Orders.TransitionStatusAsync(order.Id, OrderStatus.Shipped);
        await fx.Orders.TransitionStatusAsync(order.Id, OrderStatus.Delivered);
        await designs.PublishAsync(fx.Creator.Id, design.Id);
        return (fx, designs, design.Id);
    }

    [Fact]
    public async Task Owner_can_order_own_draft()
    {
        await using var fx = await OrderIntegrityFixture.CreateAsync();
        var design = await fx.CreateValidDesignAsync();
        // Creator owns draft; buyer is different — order as creator (owner)
        var order = await fx.Orders.CreateFromDesignAsync(fx.Creator.Id, new CreateOrderRequest(design.Id, 1, null, null));
        Assert.Equal(OrderStatus.Pending.ToString(), order.Status);
        Assert.Equal(0, await fx.Db.CreatorRewards.CountAsync());
    }

    [Fact]
    public async Task User_cannot_order_another_users_private_draft()
    {
        await using var fx = await OrderIntegrityFixture.CreateAsync();
        var design = await fx.CreateValidDesignAsync(); // owned by Creator
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            fx.Orders.CreateFromDesignAsync(fx.Buyer.Id, new CreateOrderRequest(design.Id, 1, null, null)));
    }

    [Fact]
    public async Task Cannot_order_published_design_directly_must_derive()
    {
        var (fx, _, publishedId) = await PublishReusableAsync();
        await using (fx)
        {
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                fx.Orders.CreateFromDesignAsync(fx.Buyer.Id, new CreateOrderRequest(publishedId, 1, null, null)));
        }
    }

    [Fact]
    public async Task Derive_creates_draft_owned_by_second_user_without_mutating_source()
    {
        var (fx, designs, publishedId) = await PublishReusableAsync();
        await using (fx)
        {
            var before = await fx.Db.Designs.AsNoTracking().FirstAsync(d => d.Id == publishedId);
            Assert.Equal(DesignStatus.Reusable, before.Status);
            Assert.Equal(DesignVisibility.Public, before.Visibility);

            var draft = await designs.DeriveAsync(fx.Buyer.Id, publishedId);
            Assert.Equal(fx.Buyer.Id, (await fx.Db.Designs.FirstAsync(d => d.Id == draft.Id)).OwnerId);
            Assert.Equal(publishedId, draft.DerivedFromDesignId);
            Assert.Equal(DesignStatus.Draft.ToString(), draft.Status);

            var after = await fx.Db.Designs.AsNoTracking().FirstAsync(d => d.Id == publishedId);
            Assert.Equal(DesignStatus.Reusable, after.Status);
            Assert.Equal(DesignVisibility.Public, after.Visibility);
            Assert.Equal(before.OwnerId, after.OwnerId);
            Assert.Equal(before.UpdatedAtUtc, after.UpdatedAtUtc);
        }
    }

    [Fact]
    public async Task Order_from_derived_keeps_source_reusable_and_credits_original_creator()
    {
        var (fx, designs, publishedId) = await PublishReusableAsync();
        await using (fx)
        {
            var draft = await designs.DeriveAsync(fx.Buyer.Id, publishedId);
            var order = await fx.Orders.CreateFromDesignAsync(fx.Buyer.Id, new CreateOrderRequest(draft.Id, 1, null, null));

            Assert.Equal(0, await fx.Db.CreatorRewards.CountAsync());
            var item = await fx.Db.OrderItems.AsNoTracking().FirstAsync(i => i.OrderId == order.Id);
            Assert.Equal(draft.Id, item.SourceDesignId);
            Assert.Equal(fx.Creator.Id, item.DesignerId); // original owner
            Assert.Equal(fx.Buyer.Id, (await fx.Db.Orders.AsNoTracking().FirstAsync(o => o.Id == order.Id)).BuyerId);

            var source = await fx.Db.Designs.AsNoTracking().FirstAsync(d => d.Id == publishedId);
            Assert.Equal(DesignStatus.Reusable, source.Status);
            Assert.Equal(DesignVisibility.Public, source.Visibility);

            var orderedDraft = await fx.Db.Designs.AsNoTracking().FirstAsync(d => d.Id == draft.Id);
            Assert.Equal(DesignStatus.Ordered, orderedDraft.Status);

            await fx.Orders.TransitionStatusAsync(order.Id, OrderStatus.Processing);
            await fx.Orders.TransitionStatusAsync(order.Id, OrderStatus.InProduction);
            await fx.Orders.TransitionStatusAsync(order.Id, OrderStatus.Shipped);
            await fx.Orders.TransitionStatusAsync(order.Id, OrderStatus.Delivered);

            Assert.Equal(1, await fx.Db.CreatorRewards.CountAsync(r => r.CreatorId == fx.Creator.Id && r.OrderId == order.Id));
            Assert.Equal(0, await fx.Db.CreatorRewards.CountAsync(r => r.CreatorId == fx.Buyer.Id));
            var creator = await fx.Db.Users.AsNoTracking().FirstAsync(u => u.Id == fx.Creator.Id);
            Assert.True(creator.Balance > 0);

            source = await fx.Db.Designs.AsNoTracking().FirstAsync(d => d.Id == publishedId);
            Assert.Equal(DesignStatus.Reusable, source.Status);
            Assert.Equal(DesignVisibility.Public, source.Visibility);
        }
    }

    [Fact]
    public async Task Two_users_can_reuse_same_published_design_independently()
    {
        var (fx, designs, publishedId) = await PublishReusableAsync();
        await using (fx)
        {
            var userC = new Domain.Entities.User
            {
                Name = "UserC", Email = "c@test.zouq", PasswordHash = "x", Role = UserRole.Customer
            };
            fx.Db.Users.Add(userC);
            await fx.Db.SaveChangesAsync();

            var dB = await designs.DeriveAsync(fx.Buyer.Id, publishedId);
            var dC = await designs.DeriveAsync(userC.Id, publishedId);
            Assert.NotEqual(dB.Id, dC.Id);

            var oB = await fx.Orders.CreateFromDesignAsync(fx.Buyer.Id, new CreateOrderRequest(dB.Id, 1, null, null));
            var oC = await fx.Orders.CreateFromDesignAsync(userC.Id, new CreateOrderRequest(dC.Id, 1, null, null));

            async Task Deliver(Guid orderId)
            {
                await fx.Orders.TransitionStatusAsync(orderId, OrderStatus.Processing);
                await fx.Orders.TransitionStatusAsync(orderId, OrderStatus.InProduction);
                await fx.Orders.TransitionStatusAsync(orderId, OrderStatus.Shipped);
                await fx.Orders.TransitionStatusAsync(orderId, OrderStatus.Delivered);
            }

            await Deliver(oB.Id);
            await Deliver(oC.Id);

            Assert.Equal(2, await fx.Db.CreatorRewards.CountAsync(r => r.CreatorId == fx.Creator.Id));
            Assert.Equal(1, await fx.Db.CreatorRewards.CountAsync(r => r.OrderId == oB.Id));
            Assert.Equal(1, await fx.Db.CreatorRewards.CountAsync(r => r.OrderId == oC.Id));

            var source = await fx.Db.Designs.AsNoTracking().FirstAsync(d => d.Id == publishedId);
            Assert.Equal(DesignStatus.Reusable, source.Status);
        }
    }

    [Fact]
    public async Task Three_orders_from_same_published_design_each_reward_once()
    {
        var (fx, designs, publishedId) = await PublishReusableAsync();
        await using (fx)
        {
            var users = new List<Domain.Entities.User>();
            for (var i = 0; i < 3; i++)
            {
                var u = new Domain.Entities.User
                {
                    Name = $"U{i}", Email = $"u{i}@reuse.zouq", PasswordHash = "x"
                };
                fx.Db.Users.Add(u);
                users.Add(u);
            }
            await fx.Db.SaveChangesAsync();

            var orderIds = new List<Guid>();
            foreach (var u in users)
            {
                var draft = await designs.DeriveAsync(u.Id, publishedId);
                var o = await fx.Orders.CreateFromDesignAsync(u.Id, new CreateOrderRequest(draft.Id, 1, null, null));
                orderIds.Add(o.Id);
            }

            foreach (var id in orderIds)
            {
                await fx.Orders.TransitionStatusAsync(id, OrderStatus.Processing);
                await fx.Orders.TransitionStatusAsync(id, OrderStatus.InProduction);
                await fx.Orders.TransitionStatusAsync(id, OrderStatus.Shipped);
                await fx.Orders.TransitionStatusAsync(id, OrderStatus.Delivered);
            }

            Assert.Equal(3, await fx.Db.CreatorRewards.CountAsync(r => r.CreatorId == fx.Creator.Id));
            foreach (var id in orderIds)
                Assert.Equal(1, await fx.Db.CreatorRewards.CountAsync(r => r.OrderId == id));
        }
    }

    [Fact]
    public async Task Concurrent_delivered_on_reuse_order_remains_idempotent()
    {
        var (fx, designs, publishedId) = await PublishReusableAsync();
        await using (fx)
        {
            var draft = await designs.DeriveAsync(fx.Buyer.Id, publishedId);
            var order = await fx.Orders.CreateFromDesignAsync(fx.Buyer.Id, new CreateOrderRequest(draft.Id, 1, null, null));
            await fx.Orders.TransitionStatusAsync(order.Id, OrderStatus.Processing);
            await fx.Orders.TransitionStatusAsync(order.Id, OrderStatus.InProduction);
            await fx.Orders.TransitionStatusAsync(order.Id, OrderStatus.Shipped);

            await Task.WhenAll(
                fx.Orders.TransitionStatusAsync(order.Id, OrderStatus.Delivered),
                fx.Orders.TransitionStatusAsync(order.Id, OrderStatus.Delivered));

            Assert.Equal(1, await fx.Db.CreatorRewards.CountAsync(r => r.OrderId == order.Id));
        }
    }

    [Fact]
    public async Task Private_and_undelivered_cannot_be_derived()
    {
        await using var fx = await OrderIntegrityFixture.CreateAsync();
        var designs = new DesignAppService(fx.Db, fx.Pricing, fx.Integrity);
        var draft = await fx.CreateValidDesignAsync();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            designs.DeriveAsync(fx.Buyer.Id, draft.Id));

        var order = await fx.Orders.CreateFromDesignAsync(fx.Creator.Id, new CreateOrderRequest(draft.Id, 1, null, null));
        // Ordered but not delivered
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            designs.DeriveAsync(fx.Buyer.Id, draft.Id));

        await fx.Orders.TransitionStatusAsync(order.Id, OrderStatus.Processing);
        await fx.Orders.TransitionStatusAsync(order.Id, OrderStatus.InProduction);
        await fx.Orders.TransitionStatusAsync(order.Id, OrderStatus.Shipped);
        await fx.Orders.TransitionStatusAsync(order.Id, OrderStatus.Delivered);
        // DeliveredEligible but not published
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            designs.DeriveAsync(fx.Buyer.Id, draft.Id));
    }

    [Fact]
    public async Task Refunded_source_cannot_be_derived()
    {
        var (fx, designs, publishedId) = await PublishReusableAsync();
        await using (fx)
        {
            // Find the order that made it eligible and refund — revokes Reusable on that design (source)
            var source = await fx.Db.Designs.FirstAsync(d => d.Id == publishedId);
            var orderId = source.SourceOrderId!.Value;
            await fx.Orders.TransitionStatusAsync(orderId, OrderStatus.Refunded);

            source = await fx.Db.Designs.AsNoTracking().FirstAsync(d => d.Id == publishedId);
            Assert.NotEqual(DesignStatus.Reusable, source.Status);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                designs.DeriveAsync(fx.Buyer.Id, publishedId));
        }
    }

    [Fact]
    public async Task Modifying_derived_does_not_modify_source()
    {
        var (fx, designs, publishedId) = await PublishReusableAsync();
        await using (fx)
        {
            var draftDto = await designs.DeriveAsync(fx.Buyer.Id, publishedId);
            var sourceElBefore = await fx.Db.DesignElements.AsNoTracking()
                .Where(e => e.DesignId == publishedId).Select(e => e.NormX).FirstAsync();

            fx.Db.ChangeTracker.Clear();

            var draft = await fx.Db.Designs.Include(d => d.Elements).FirstAsync(d => d.Id == draftDto.Id);
            draft.Title = "Modified";
            var first = draft.Elements.First();
            first.NormX = 0.1;
            first.NormY = 0.1;
            first.NormWidth = 0.2;
            first.NormHeight = 0.2;
            await fx.Db.SaveChangesAsync();

            var sourceElAfter = await fx.Db.DesignElements.AsNoTracking()
                .Where(e => e.DesignId == publishedId).Select(e => e.NormX).FirstAsync();
            Assert.Equal(sourceElBefore, sourceElAfter);

            var derived = await fx.Db.Designs.AsNoTracking().FirstAsync(d => d.Id == draftDto.Id);
            Assert.Equal(publishedId, derived.DerivedFromDesignId);
            Assert.Equal("Modified", derived.Title);
            Assert.Equal(0.1, await fx.Db.DesignElements.AsNoTracking()
                .Where(e => e.DesignId == draftDto.Id).Select(e => e.NormX).FirstAsync());
        }
    }
}
