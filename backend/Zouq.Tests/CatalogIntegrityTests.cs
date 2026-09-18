using Microsoft.EntityFrameworkCore;
using Zouq.Application.DTOs;
using Zouq.Domain.Enums;

namespace Zouq.Tests;

public class CatalogIntegrityTests
{
    [Fact]
    public async Task Invalid_fabric_rejected()
    {
        await using var fx = await OrderIntegrityFixture.CreateAsync();
        var design = await fx.CreateValidDesignAsync();
        var orphan = new Zouq.Domain.Entities.Fabric
        {
            Name = "Orphan",
            PriceAdjustment = 1,
            Status = Zouq.Domain.Enums.EntityStatus.Active
        };
        fx.Db.Fabrics.Add(orphan);
        await fx.Db.SaveChangesAsync();
        design.FabricId = orphan.Id; // exists but not linked to product
        await fx.Db.SaveChangesAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fx.Orders.CreateFromDesignAsync(fx.Creator.Id, new CreateOrderRequest(design.Id, 1, null, null)));
    }

    [Fact]
    public async Task Element_outside_design_area_rejected()
    {
        await using var fx = await OrderIntegrityFixture.CreateAsync();
        var design = await fx.CreateValidDesignAsync();
        var el = design.Elements.First();
        el.NormX = 0.8;
        el.NormWidth = 0.5; // exceeds 1.0
        await fx.Db.SaveChangesAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fx.Orders.CreateFromDesignAsync(fx.Creator.Id, new CreateOrderRequest(design.Id, 1, null, null)));
    }

    [Fact]
    public async Task Server_recomputes_real_dimensions_from_norms()
    {
        await using var fx = await OrderIntegrityFixture.CreateAsync();
        // Client claims huge size — server overwrites from norms × area (0.5 × 30 = 15)
        var design = await fx.CreateValidDesignAsync(fakeClientRealWidth: 999, fakeClientRealHeight: 999);
        var order = await fx.Orders.CreateFromDesignAsync(fx.Creator.Id, new CreateOrderRequest(design.Id, 1, null, null));
        var item = await fx.Db.OrderItems.AsNoTracking().FirstAsync(i => i.OrderId == order.Id);
        Assert.Contains("15", item.DesignSnapshotJson);
        Assert.DoesNotContain("999", item.DesignSnapshotJson);
    }

    [Fact]
    public async Task Self_order_does_not_credit_creator_on_delivered()
    {
        await using var fx = await FinancialTestFixture.CreateAsync();
        var design = await fx.CreateDesignAsync(fx.Buyer.Id);
        var order = await fx.Orders.CreateFromDesignAsync(fx.Buyer.Id, new CreateOrderRequest(design.Id, 1, null, null));
        await fx.AdvanceToAsync(order.Id, OrderStatus.Delivered);
        Assert.Equal(0, await fx.Db.CreatorRewards.CountAsync(r => r.OrderId == order.Id));
        var buyer = await fx.Db.Users.AsNoTracking().FirstAsync(u => u.Id == fx.Buyer.Id);
        Assert.Equal(0, buyer.Balance);
    }
}
