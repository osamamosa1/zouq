using Microsoft.EntityFrameworkCore;
using Zouq.Application.DTOs;
using Zouq.Domain.Enums;

namespace Zouq.Tests;

public class DesignLifecycleTests
{
    [Fact]
    public async Task Draft_is_not_in_for_you()
    {
        await using var fx = await OrderIntegrityFixture.CreateAsync();
        var design = await fx.CreateValidDesignAsync();
        // Draft/Published without DeliveredEligible+Reusable must not appear
        design.Status = DesignStatus.Draft;
        design.Visibility = DesignVisibility.Public;
        await fx.Db.SaveChangesAsync();

        var feed = new Zouq.Infrastructure.Services.FeedAppService(fx.Db);
        var items = await feed.ForYouAsync(null);
        Assert.DoesNotContain(items, i => i.DesignId == design.Id);
    }

    [Fact]
    public async Task Ordered_design_not_in_for_you_until_delivered_and_published()
    {
        await using var fx = await OrderIntegrityFixture.CreateAsync();
        var design = await fx.CreateValidDesignAsync();
        var order = await fx.Orders.CreateFromDesignAsync(fx.Creator.Id, new CreateOrderRequest(design.Id, 1, null, null));

        var feed = new Zouq.Infrastructure.Services.FeedAppService(fx.Db);
        Assert.DoesNotContain(await feed.ForYouAsync(null), i => i.DesignId == design.Id);

        // Attempt publish before Delivered must fail
        var designs = new Zouq.Infrastructure.Services.DesignAppService(fx.Db, fx.Pricing, fx.Integrity);
        await Assert.ThrowsAsync<InvalidOperationException>(() => designs.PublishAsync(fx.Creator.Id, design.Id));

        await fx.Orders.TransitionStatusAsync(order.Id, OrderStatus.Processing);
        await fx.Orders.TransitionStatusAsync(order.Id, OrderStatus.InProduction);
        await fx.Orders.TransitionStatusAsync(order.Id, OrderStatus.Shipped);
        await fx.Orders.TransitionStatusAsync(order.Id, OrderStatus.Delivered);

        var eligible = await fx.Db.Designs.AsNoTracking().FirstAsync(d => d.Id == design.Id);
        Assert.Equal(DesignStatus.DeliveredEligible, eligible.Status);
        Assert.Equal(DesignVisibility.Private, eligible.Visibility);
        Assert.DoesNotContain(await feed.ForYouAsync(null), i => i.DesignId == design.Id);

        await designs.PublishAsync(fx.Creator.Id, design.Id);
        var items = await feed.ForYouAsync(null);
        Assert.Contains(items, i => i.DesignId == design.Id);
    }

    [Fact]
    public async Task Refund_revokes_publication_eligibility()
    {
        await using var fx = await OrderIntegrityFixture.CreateAsync();
        var design = await fx.CreateValidDesignAsync();
        var order = await fx.Orders.CreateFromDesignAsync(fx.Creator.Id, new CreateOrderRequest(design.Id, 1, null, null));
        await fx.Orders.TransitionStatusAsync(order.Id, OrderStatus.Processing);
        await fx.Orders.TransitionStatusAsync(order.Id, OrderStatus.InProduction);
        await fx.Orders.TransitionStatusAsync(order.Id, OrderStatus.Shipped);
        await fx.Orders.TransitionStatusAsync(order.Id, OrderStatus.Delivered);

        var designs = new Zouq.Infrastructure.Services.DesignAppService(fx.Db, fx.Pricing, fx.Integrity);
        await designs.PublishAsync(fx.Creator.Id, design.Id);

        await fx.Orders.TransitionStatusAsync(order.Id, OrderStatus.Refunded);
        var after = await fx.Db.Designs.AsNoTracking().FirstAsync(d => d.Id == design.Id);
        Assert.Equal(DesignStatus.Ordered, after.Status);
        Assert.Equal(DesignVisibility.Private, after.Visibility);

        var feed = new Zouq.Infrastructure.Services.FeedAppService(fx.Db);
        Assert.DoesNotContain(await feed.ForYouAsync(null), i => i.DesignId == design.Id);
    }
}
