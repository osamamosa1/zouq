using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Zouq.Domain.Entities;
using Zouq.Domain.Enums;
using Zouq.Infrastructure.Data;
using Zouq.Infrastructure.Services;

namespace Zouq.Tests;

public sealed class FinancialTestFixture : IAsyncDisposable
{
    private readonly SqliteConnection _connection;
    public ZouqDbContext Db { get; }
    public FinancialLedgerService Ledger { get; }
    public OrderAppService Orders { get; }

    public User Buyer { get; private set; } = null!;
    public User Creator { get; private set; } = null!;
    public User Admin { get; private set; } = null!;
    public Product Product { get; private set; } = null!;

    private FinancialTestFixture(SqliteConnection connection, ZouqDbContext db)
    {
        _connection = connection;
        Db = db;
        Ledger = new FinancialLedgerService(db, NullLogger<FinancialLedgerService>.Instance);
        var integrity = new DesignIntegrityService(db);
        Orders = new OrderAppService(db, new StubPricing(), Ledger, integrity, NullLogger<OrderAppService>.Instance);
    }

    public static async Task<FinancialTestFixture> CreateAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ZouqDbContext>()
            .UseSqlite(connection)
            .Options;
        var db = new ZouqDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var fx = new FinancialTestFixture(connection, db);
        await fx.SeedAsync();
        return fx;
    }

    private async Task SeedAsync()
    {
        Buyer = new User { Name = "Buyer", Email = "buyer@test.zouq", PasswordHash = "x", Role = UserRole.Customer };
        Creator = new User { Name = "Creator", Email = "creator@test.zouq", PasswordHash = "x", Role = UserRole.Customer };
        Admin = new User { Name = "Admin", Email = "admin@test.zouq", PasswordHash = "x", Role = UserRole.Admin };
        Db.Users.AddRange(Buyer, Creator, Admin);

        Db.CommissionSettings.Add(new CommissionSetting
        {
            Name = "default",
            DesignerCommissionPercent = 10m,
            IsActive = true
        });

        var type = new ProductType { Name = "Apparel", Slug = "apparel", Status = EntityStatus.Active };
        Product = new Product
        {
            ProductType = type,
            Name = "Tee",
            Slug = "tee",
            BasePrice = 100m,
            Status = EntityStatus.Active,
            MeasurementUnit = MeasurementUnit.Centimeter
        };
        Db.Products.Add(Product);
        await Db.SaveChangesAsync();
    }

    public async Task<Design> CreateDesignAsync(Guid? ownerId = null)
    {
        // Self-order path: draft owned by the given user (typically Buyer) — no creator attribution.
        if (ownerId is Guid oid && oid == Buyer.Id)
        {
            var self = new Design
            {
                OwnerId = Buyer.Id,
                ProductId = Product.Id,
                Title = "Self design",
                Status = DesignStatus.Draft,
                Visibility = DesignVisibility.Private
            };
            Db.Designs.Add(self);
            await Db.SaveChangesAsync();
            return self;
        }

        // Third-party reward path: Creator owns attribution source; Buyer owns orderable draft.
        var source = new Design
        {
            OwnerId = Creator.Id,
            ProductId = Product.Id,
            Title = "Source",
            Status = DesignStatus.Draft,
            Visibility = DesignVisibility.Private
        };
        Db.Designs.Add(source);
        await Db.SaveChangesAsync();

        var draft = new Design
        {
            OwnerId = Buyer.Id,
            ProductId = Product.Id,
            Title = "Design",
            Status = DesignStatus.Draft,
            Visibility = DesignVisibility.Private,
            DerivedFromDesignId = source.Id
        };
        Db.Designs.Add(draft);
        await Db.SaveChangesAsync();
        return draft;
    }

    public async Task AdvanceToAsync(Guid orderId, OrderStatus target)
    {
        var path = new[]
        {
            OrderStatus.Processing,
            OrderStatus.InProduction,
            OrderStatus.Shipped,
            OrderStatus.Delivered
        };

        var order = await Db.Orders.AsNoTracking().FirstAsync(o => o.Id == orderId);
        var current = order.Status;
        foreach (var step in path)
        {
            if (current == target) return;
            if ((int)step <= (int)current) continue;
            if ((int)step > (int)target) break;
            await Orders.TransitionStatusAsync(orderId, step);
            current = step;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await Db.DisposeAsync();
        await _connection.DisposeAsync();
    }

    private sealed class StubPricing : Application.Interfaces.IPriceCalculationService
    {
        public Task<Application.Interfaces.PriceBreakdown> CalculateDesignPriceAsync(
            Application.Interfaces.PriceCalculationRequest request,
            CancellationToken ct = default)
        {
            var total = 200m;
            Application.Interfaces.PriceBreakdown breakdown = new(
                200m, 0, 0, 0, 0, 0, total,
                new List<Application.Interfaces.PriceLine> { new("base", "Base", 200m) });
            return Task.FromResult(breakdown);
        }
    }
}
