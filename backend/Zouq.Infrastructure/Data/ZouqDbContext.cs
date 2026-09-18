using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Zouq.Domain.Entities;

namespace Zouq.Infrastructure.Data;

public class ZouqDbContext : DbContext
{
    public ZouqDbContext(DbContextOptions<ZouqDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<UserInterestTag> UserInterestTags => Set<UserInterestTag>();
    public DbSet<ProductType> ProductTypes => Set<ProductType>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductSurface> ProductSurfaces => Set<ProductSurface>();
    public DbSet<ProductDesignArea> ProductDesignAreas => Set<ProductDesignArea>();
    public DbSet<Fabric> Fabrics => Set<Fabric>();
    public DbSet<ProductFabric> ProductFabrics => Set<ProductFabric>();
    public DbSet<CutStyle> CutStyles => Set<CutStyle>();
    public DbSet<ProductCut> ProductCuts => Set<ProductCut>();
    public DbSet<ProductSize> ProductSizes => Set<ProductSize>();
    public DbSet<DesignAssetCategory> DesignAssetCategories => Set<DesignAssetCategory>();
    public DbSet<DesignAsset> DesignAssets => Set<DesignAsset>();
    public DbSet<UserUpload> UserUploads => Set<UserUpload>();
    public DbSet<Design> Designs => Set<Design>();
    public DbSet<DesignElement> DesignElements => Set<DesignElement>();
    public DbSet<PrintingOption> PrintingOptions => Set<PrintingOption>();
    public DbSet<EmbroideryPricing> EmbroideryPricings => Set<EmbroideryPricing>();
    public DbSet<CommissionSetting> CommissionSettings => Set<CommissionSetting>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<CreatorReward> CreatorRewards => Set<CreatorReward>();
    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();
    public DbSet<Advertisement> Advertisements => Set<Advertisement>();
    public DbSet<PlatformSetting> PlatformSettings => Set<PlatformSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var stringListConverter = new ValueConverter<List<string>, string>(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>());

        var guidListConverter = new ValueConverter<List<Guid>, string>(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions?)null) ?? new List<Guid>());

        var dictConverter = new ValueConverter<Dictionary<string, string>, string>(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, string>());

        var stringListComparer = new ValueComparer<List<string>>(
            (a, b) => a!.SequenceEqual(b!),
            v => v.Aggregate(0, (h, s) => HashCode.Combine(h, s.GetHashCode())),
            v => v.ToList());

        var guidListComparer = new ValueComparer<List<Guid>>(
            (a, b) => a!.SequenceEqual(b!),
            v => v.Aggregate(0, (h, g) => HashCode.Combine(h, g.GetHashCode())),
            v => v.ToList());

        var dictComparer = new ValueComparer<Dictionary<string, string>>(
            (a, b) => a!.Count == b!.Count && !a.Except(b).Any(),
            v => v.Aggregate(0, (h, kv) => HashCode.Combine(h, kv.Key.GetHashCode(), kv.Value.GetHashCode())),
            v => new Dictionary<string, string>(v));

        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Email).HasMaxLength(256);
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.Balance).HasPrecision(18, 2);
            e.Property(x => x.ConcurrencyStamp).IsConcurrencyToken();
        });

        modelBuilder.Entity<Product>(e =>
        {
            e.HasIndex(x => x.Slug).IsUnique();
            e.Property(x => x.BasePrice).HasPrecision(18, 2);
            e.HasOne(x => x.EmbroideryPricing).WithOne(x => x.Product)
                .HasForeignKey<EmbroideryPricing>(x => x.ProductId);
        });

        modelBuilder.Entity<ProductSurface>(e =>
        {
            e.HasIndex(x => new { x.ProductId, x.Code }).IsUnique();
            e.HasOne(x => x.DesignArea).WithOne(x => x.ProductSurface)
                .HasForeignKey<ProductDesignArea>(x => x.ProductSurfaceId);
        });

        modelBuilder.Entity<ProductSize>(e =>
        {
            e.Property(x => x.Width).HasPrecision(18, 4);
            e.Property(x => x.Height).HasPrecision(18, 4);
            e.Property(x => x.Depth).HasPrecision(18, 4);
            e.Property(x => x.PriceAdjustment).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Fabric>(e =>
        {
            e.Property(x => x.PriceAdjustment).HasPrecision(18, 2);
            e.Property(x => x.Metadata).HasConversion(dictConverter).Metadata.SetValueComparer(dictComparer);
        });

        modelBuilder.Entity<CutStyle>(e => e.Property(x => x.PriceAdjustment).HasPrecision(18, 2));

        modelBuilder.Entity<ProductDesignArea>(e =>
        {
            e.Property(x => x.RealWidth).HasPrecision(18, 4);
            e.Property(x => x.RealHeight).HasPrecision(18, 4);
        });

        modelBuilder.Entity<PrintingOption>(e =>
        {
            e.Property(x => x.Price).HasPrecision(18, 2);
            e.Property(x => x.IncludedSurfaceCodes).HasConversion(stringListConverter).Metadata.SetValueComparer(stringListComparer);
        });

        modelBuilder.Entity<EmbroideryPricing>(e =>
        {
            e.Property(x => x.PricePerSquareUnit).HasPrecision(18, 4);
            e.Property(x => x.MinimumCharge).HasPrecision(18, 2);
        });

        modelBuilder.Entity<DesignAsset>(e =>
        {
            e.Property(x => x.Tags).HasConversion(stringListConverter).Metadata.SetValueComparer(stringListComparer);
            e.Property(x => x.CompatibleProductIds).HasConversion(guidListConverter).Metadata.SetValueComparer(guidListComparer);
            e.Property(x => x.CompatibleSurfaceCodes).HasConversion(stringListConverter).Metadata.SetValueComparer(stringListComparer);
        });

        modelBuilder.Entity<Design>(e =>
        {
            e.Property(x => x.Tags).HasConversion(stringListConverter).Metadata.SetValueComparer(stringListComparer);
            e.Property(x => x.LastEstimatedPrice).HasPrecision(18, 2);
            e.HasOne(x => x.Owner).WithMany().HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.SourceOrder).WithMany().HasForeignKey(x => x.SourceOrderId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.DerivedFromDesign).WithMany().HasForeignKey(x => x.DerivedFromDesignId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.Status, x.Visibility, x.IsFeatured });
            e.HasIndex(x => x.OwnerId);
            e.HasIndex(x => x.DerivedFromDesignId);
        });

        modelBuilder.Entity<DesignElement>(e =>
        {
            e.Property(x => x.RealWidth).HasPrecision(18, 4);
            e.Property(x => x.RealHeight).HasPrecision(18, 4);
            e.Property(x => x.Metadata).HasConversion(dictConverter).Metadata.SetValueComparer(dictComparer);
        });

        modelBuilder.Entity<UserInterestTag>(e =>
        {
            e.HasIndex(x => new { x.UserId, x.Tag }).IsUnique();
            e.Property(x => x.Tag).HasMaxLength(64);
        });

        modelBuilder.Entity<Order>(e =>
        {
            e.HasIndex(x => x.OrderNumber).IsUnique();
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.BuyerId);
            e.Property(x => x.Subtotal).HasPrecision(18, 2);
            e.Property(x => x.Total).HasPrecision(18, 2);
            e.Property(x => x.CommissionPercentSnapshot).HasPrecision(18, 4);
            e.Property(x => x.CreatorRewardAmount).HasPrecision(18, 2);
            e.HasOne(x => x.Buyer).WithMany().HasForeignKey(x => x.BuyerId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Advertisement>(e =>
        {
            e.HasIndex(x => new { x.IsActive, x.Placement, x.DisplayPriority });
        });

        modelBuilder.Entity<UserUpload>(e =>
        {
            e.Property(x => x.AiTags).HasConversion(stringListConverter).Metadata.SetValueComparer(stringListComparer);
            e.HasIndex(x => x.UserId);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrderItem>(e =>
        {
            e.Property(x => x.UnitPrice).HasPrecision(18, 2);
            e.Property(x => x.LineTotal).HasPrecision(18, 2);
            e.HasOne(x => x.Order).WithMany(o => o.Items).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.SourceDesign).WithMany().HasForeignKey(x => x.SourceDesignId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Designer).WithMany().HasForeignKey(x => x.DesignerId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CreatorReward>(e =>
        {
            e.Property(x => x.CommissionPercent).HasPrecision(18, 4);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            // DB-level idempotency: one reward row per order+creator
            e.HasIndex(x => new { x.OrderId, x.CreatorId }).IsUnique();
            e.HasOne(x => x.Order).WithMany(o => o.CreatorRewards).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.OrderItem).WithMany().HasForeignKey(x => x.OrderItemId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Creator).WithMany().HasForeignKey(x => x.CreatorId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Design).WithMany().HasForeignKey(x => x.DesignId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.CreditLedgerEntry).WithMany().HasForeignKey(x => x.CreditLedgerEntryId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ReversalLedgerEntry).WithMany().HasForeignKey(x => x.ReversalLedgerEntryId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<LedgerEntry>(e =>
        {
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.BalanceAfter).HasPrecision(18, 2);
            e.Property(x => x.CommissionPercent).HasPrecision(18, 4);
            e.Property(x => x.Reference).HasMaxLength(128).IsRequired();
            e.HasIndex(x => x.Reference).IsUnique();
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.OrderItem).WithMany().HasForeignKey(x => x.OrderItemId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Design).WithMany().HasForeignKey(x => x.DesignId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Creator).WithMany().HasForeignKey(x => x.CreatorId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.CreatedByAdmin).WithMany().HasForeignKey(x => x.CreatedByAdminId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CommissionSetting>(e =>
            e.Property(x => x.DesignerCommissionPercent).HasPrecision(18, 4));

        modelBuilder.Entity<PlatformSetting>(e =>
            e.HasIndex(x => x.Key).IsUnique());

        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.HasOne(x => x.User).WithMany(u => u.RefreshTokens).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Never soft-delete or hard-delete ledger rows through normal change tracking misuse
        foreach (var entry in ChangeTracker.Entries<LedgerEntry>())
        {
            if (entry.State == EntityState.Deleted)
                throw new InvalidOperationException("Ledger entries are immutable and cannot be deleted. Create a reversal instead.");
            if (entry.State == EntityState.Modified && entry.Entity.IsDeleted)
                throw new InvalidOperationException("Ledger entries cannot be soft-deleted.");
        }

        foreach (var entry in ChangeTracker.Entries<Domain.Common.AuditableEntity>())
        {
            if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedAtUtc = DateTime.UtcNow;
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}
