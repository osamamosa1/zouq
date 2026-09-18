using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;
using Zouq.Application.DTOs;
using Zouq.Application.Interfaces;
using Zouq.Domain.Entities;
using Zouq.Domain.Enums;
using Zouq.Infrastructure.Data;
using Zouq.Infrastructure.Services;

namespace Zouq.Tests;

public class OrderIntegrityTests
{
    [Fact]
    public async Task Catalog_price_change_after_order_does_not_change_order_price()
    {
        await using var fx = await OrderIntegrityFixture.CreateAsync();
        var design = await fx.CreateValidDesignAsync();
        var order = await fx.Orders.CreateFromDesignAsync(fx.Creator.Id, new CreateOrderRequest(design.Id, 1, null, null));
        var frozenTotal = order.Total;

        fx.Product.BasePrice = 9999m;
        await fx.Db.SaveChangesAsync();

        var reloaded = await fx.Db.Orders.AsNoTracking().Include(o => o.Items).FirstAsync(o => o.Id == order.Id);
        Assert.Equal(frozenTotal, reloaded.Total);
        Assert.Equal(frozenTotal, reloaded.Items.Single().UnitPrice);
    }

    [Fact]
    public async Task Design_modified_after_order_is_rejected_and_snapshot_unchanged()
    {
        await using var fx = await OrderIntegrityFixture.CreateAsync();
        var design = await fx.CreateValidDesignAsync();
        var order = await fx.Orders.CreateFromDesignAsync(fx.Creator.Id, new CreateOrderRequest(design.Id, 1, null, null));
        var snapshot = (await fx.Db.OrderItems.AsNoTracking().FirstAsync(i => i.OrderId == order.Id)).DesignSnapshotJson;

        var designs = new DesignAppService(fx.Db, fx.Pricing, fx.Integrity);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            designs.SaveAsync(fx.Creator.Id, new SaveDesignRequest(
                fx.Product.Id, fx.Fabric.Id, fx.Cut.Id, fx.Size.Id, fx.Print.Id,
                "Hacked", null, "Draft", "Private",
                new[]
                {
                    new DesignElementInputDto("front", null, null, "Printing",
                        0.1, 0.1, 0.2, 0.2, 0, 1, 999, 999, 0)
                }), design.Id));

        var after = (await fx.Db.OrderItems.AsNoTracking().FirstAsync(i => i.OrderId == order.Id)).DesignSnapshotJson;
        Assert.Equal(snapshot, after);

        var locked = await fx.Db.Designs.AsNoTracking().FirstAsync(d => d.Id == design.Id);
        Assert.Equal(DesignStatus.Ordered, locked.Status);
    }

    [Fact]
    public async Task Client_fake_RealWidth_is_ignored_server_recomputes()
    {
        await using var fx = await OrderIntegrityFixture.CreateAsync();
        var design = await fx.CreateValidDesignAsync(fakeClientRealWidth: 9999m, fakeClientRealHeight: 9999m);

        // Before order, stored fake values exist on entity â€” order path must overwrite
        Assert.Equal(9999m, design.Elements.First().RealWidth);

        var order = await fx.Orders.CreateFromDesignAsync(fx.Creator.Id, new CreateOrderRequest(design.Id, 1, null, null));
        var item = await fx.Db.OrderItems.AsNoTracking().FirstAsync(i => i.OrderId == order.Id);
        using var doc = JsonDocument.Parse(item.DesignSnapshotJson);
        var el = doc.RootElement.GetProperty("Elements")[0];
        // Area real 30Ã—35, norms 0.5Ã—0.5 â†’ 15Ã—17.5
        Assert.Equal(15m, el.GetProperty("RealWidth").GetDecimal());
        Assert.Equal(17.5m, el.GetProperty("RealHeight").GetDecimal());

        var persisted = await fx.Db.DesignElements.AsNoTracking().FirstAsync(e => e.DesignId == design.Id);
        Assert.Equal(15m, persisted.RealWidth);
        Assert.Equal(17.5m, persisted.RealHeight);
    }

    [Fact]
    public async Task Invalid_fabric_for_product_rejects_order()
    {
        await using var fx = await OrderIntegrityFixture.CreateAsync();
        var foreignFabric = new Fabric { Name = "Alien", PriceAdjustment = 0, Status = EntityStatus.Active };
        fx.Db.Fabrics.Add(foreignFabric);
        await fx.Db.SaveChangesAsync();

        var design = await fx.CreateValidDesignAsync();
        design.FabricId = foreignFabric.Id;
        await fx.Db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fx.Orders.CreateFromDesignAsync(fx.Creator.Id, new CreateOrderRequest(design.Id, 1, null, null)));
        Assert.Contains("Fabric", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Invalid_cut_for_product_rejects_order()
    {
        await using var fx = await OrderIntegrityFixture.CreateAsync();
        var foreignCut = new CutStyle { Name = "AlienCut", PriceAdjustment = 0, Status = EntityStatus.Active };
        fx.Db.CutStyles.Add(foreignCut);
        await fx.Db.SaveChangesAsync();

        var design = await fx.CreateValidDesignAsync();
        design.CutStyleId = foreignCut.Id;
        await fx.Db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fx.Orders.CreateFromDesignAsync(fx.Creator.Id, new CreateOrderRequest(design.Id, 1, null, null)));
        Assert.Contains("Cut", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Element_outside_design_area_rejects_order()
    {
        await using var fx = await OrderIntegrityFixture.CreateAsync();
        var design = await fx.CreateValidDesignAsync();
        var el = design.Elements.First();
        el.NormX = 0.8;
        el.NormY = 0.8;
        el.NormWidth = 0.5;  // 0.8+0.5 > 1
        el.NormHeight = 0.5;
        await fx.Db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fx.Orders.CreateFromDesignAsync(fx.Creator.Id, new CreateOrderRequest(design.Id, 1, null, null)));
        Assert.Contains("outside the design area", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class OrderIntegrityFixture : IAsyncDisposable
{
    private readonly SqliteConnection _connection;
    public ZouqDbContext Db { get; }
    public OrderAppService Orders { get; }
    public DesignIntegrityService Integrity { get; }
    public IPriceCalculationService Pricing { get; }

    public User Buyer { get; private set; } = null!;
    public User Creator { get; private set; } = null!;
    public Product Product { get; private set; } = null!;
    public Fabric Fabric { get; private set; } = null!;
    public CutStyle Cut { get; private set; } = null!;
    public ProductSize Size { get; private set; } = null!;
    public PrintingOption Print { get; private set; } = null!;

    private OrderIntegrityFixture(SqliteConnection connection, ZouqDbContext db)
    {
        _connection = connection;
        Db = db;
        Integrity = new DesignIntegrityService(db);
        Pricing = new PriceCalculationService(db);
        Orders = new OrderAppService(db, Pricing, new FinancialLedgerService(db, NullLogger<FinancialLedgerService>.Instance), Integrity, NullLogger<OrderAppService>.Instance);
    }

    public static async Task<OrderIntegrityFixture> CreateAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ZouqDbContext>().UseSqlite(connection).Options;
        var db = new ZouqDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var fx = new OrderIntegrityFixture(connection, db);
        await fx.SeedAsync();
        return fx;
    }

    private async Task SeedAsync()
    {
        Buyer = new User { Name = "Buyer", Email = "buyer2@test.zouq", PasswordHash = "x" };
        Creator = new User { Name = "Creator", Email = "creator2@test.zouq", PasswordHash = "x" };
        Db.Users.AddRange(Buyer, Creator);
        Db.CommissionSettings.Add(new CommissionSetting { Name = "default", DesignerCommissionPercent = 10, IsActive = true });

        Fabric = new Fabric { Name = "Cotton", PriceAdjustment = 10, Status = EntityStatus.Active };
        Cut = new CutStyle { Name = "Regular", PriceAdjustment = 5, Status = EntityStatus.Active };
        var type = new ProductType { Name = "Apparel", Slug = "apparel2", Status = EntityStatus.Active };
        Product = new Product
        {
            ProductType = type,
            Name = "Tee2",
            Slug = "tee-2",
            BasePrice = 100,
            Status = EntityStatus.Active,
            MeasurementUnit = MeasurementUnit.Centimeter
        };
        Product.Surfaces.Add(new ProductSurface
        {
            Code = "front",
            Name = "Front",
            Status = EntityStatus.Active,
            DesignArea = new ProductDesignArea
            {
                NormX = 0.25, NormY = 0.2, NormWidth = 0.5, NormHeight = 0.45,
                RealWidth = 30, RealHeight = 35
            }
        });
        Product.ProductFabrics.Add(new ProductFabric { Fabric = Fabric, IsDefault = true });
        Product.ProductCuts.Add(new ProductCut { CutStyle = Cut, IsDefault = true });
        Size = new ProductSize
        {
            Code = "M", Name = "Medium", Width = 50, Height = 70,
            Unit = MeasurementUnit.Centimeter, Status = EntityStatus.Active
        };
        Product.Sizes.Add(Size);
        Print = new PrintingOption
        {
            Name = "Front only",
            Code = "front_only",
            Price = 40,
            IncludedSurfaceCodes = new List<string> { "front" },
            Status = EntityStatus.Active
        };
        Product.PrintingOptions.Add(Print);
        Product.EmbroideryPricing = new EmbroideryPricing
        {
            PricePerSquareUnit = 1m,
            Unit = MeasurementUnit.Centimeter,
            Status = EntityStatus.Active
        };
        Db.Products.Add(Product);
        await Db.SaveChangesAsync();
    }

    public async Task<Design> CreateValidDesignAsync(decimal fakeClientRealWidth = 15m, decimal fakeClientRealHeight = 17.5m)
    {
        var design = new Design
        {
            OwnerId = Creator.Id,
            ProductId = Product.Id,
            FabricId = Fabric.Id,
            CutStyleId = Cut.Id,
            ProductSizeId = Size.Id,
            PrintingOptionId = Print.Id,
            Title = "Valid",
            Status = DesignStatus.Draft,
            Visibility = DesignVisibility.Private,
            Elements =
            {
                new DesignElement
                {
                    SurfaceCode = "front",
                    ProductionMethod = ProductionMethodCode.Printing,
                    NormX = 0.25, NormY = 0.25, NormWidth = 0.5, NormHeight = 0.5,
                    RealWidth = fakeClientRealWidth,
                    RealHeight = fakeClientRealHeight
                }
            }
        };
        Db.Designs.Add(design);
        await Db.SaveChangesAsync();
        return await Db.Designs.Include(d => d.Elements).FirstAsync(d => d.Id == design.Id);
    }

    public async ValueTask DisposeAsync()
    {
        await Db.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
