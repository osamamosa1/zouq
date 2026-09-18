using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Zouq.Application.Interfaces;
using Zouq.Domain.Entities;
using Zouq.Domain.Enums;

namespace Zouq.Infrastructure.Data;

public static class ZouqSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ZouqDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("ZouqSeeder");

        await db.Database.MigrateAsync();

        if (!await db.Users.AnyAsync(u => u.Role == UserRole.Admin))
        {
            db.Users.Add(new User
            {
                Name = "Zouq Admin",
                Email = "admin@zouq.app",
                PasswordHash = hasher.Hash("Admin@12345"),
                Role = UserRole.Admin,
                IsActive = true
            });
            logger.LogInformation("Seeded admin user admin@zouq.app");
        }

        if (!await db.CommissionSettings.AnyAsync())
        {
            db.CommissionSettings.Add(new CommissionSetting
            {
                Name = "default",
                DesignerCommissionPercent = 10,
                IsActive = true
            });
        }

        if (!await db.Products.AnyAsync())
        {
            var type = new ProductType
            {
                Name = "Apparel",
                Slug = "apparel",
                Description = "Wearable customizable products",
                Status = EntityStatus.Active
            };

            var cotton = new Fabric
            {
                Name = "Cotton",
                Description = "Soft cotton fabric",
                PriceAdjustment = 0,
                Status = EntityStatus.Active
            };
            var premium = new Fabric
            {
                Name = "Premium Cotton",
                Description = "Heavier premium cotton",
                PriceAdjustment = 25,
                Status = EntityStatus.Active
            };

            var regular = new CutStyle { Name = "Regular", PriceAdjustment = 0, Status = EntityStatus.Active };
            var oversized = new CutStyle { Name = "Oversized", PriceAdjustment = 15, Status = EntityStatus.Active };
            var slim = new CutStyle { Name = "Slim", PriceAdjustment = 10, Status = EntityStatus.Active };

            var product = new Product
            {
                ProductType = type,
                Name = "Classic T-Shirt",
                Slug = "classic-t-shirt",
                Description = "Customizable t-shirt — first product on zouq. Architecture supports hoodies, bags, caps, etc.",
                BasePrice = 150,
                MeasurementUnit = MeasurementUnit.Centimeter,
                Status = EntityStatus.Active,
                SortOrder = 1
            };

            var front = new ProductSurface
            {
                Code = "front",
                Name = "Front",
                SortOrder = 1,
                IsRequired = false,
                Status = EntityStatus.Active,
                DesignArea = new ProductDesignArea
                {
                    NormX = 0.25, NormY = 0.20, NormWidth = 0.50, NormHeight = 0.45,
                    RealWidth = 30, RealHeight = 35
                }
            };
            var back = new ProductSurface
            {
                Code = "back",
                Name = "Back",
                SortOrder = 2,
                IsRequired = false,
                Status = EntityStatus.Active,
                DesignArea = new ProductDesignArea
                {
                    NormX = 0.25, NormY = 0.18, NormWidth = 0.50, NormHeight = 0.48,
                    RealWidth = 30, RealHeight = 38
                }
            };

            product.Surfaces.Add(front);
            product.Surfaces.Add(back);

            product.ProductFabrics.Add(new ProductFabric { Fabric = cotton, IsDefault = true });
            product.ProductFabrics.Add(new ProductFabric { Fabric = premium, IsDefault = false });
            product.ProductCuts.Add(new ProductCut { CutStyle = regular, IsDefault = true });
            product.ProductCuts.Add(new ProductCut { CutStyle = oversized });
            product.ProductCuts.Add(new ProductCut { CutStyle = slim });

            product.Sizes.Add(new ProductSize { Code = "S", Name = "Small", Width = 46, Height = 68, Unit = MeasurementUnit.Centimeter, SortOrder = 1 });
            product.Sizes.Add(new ProductSize { Code = "M", Name = "Medium", Width = 50, Height = 70, Unit = MeasurementUnit.Centimeter, SortOrder = 2 });
            product.Sizes.Add(new ProductSize { Code = "L", Name = "Large", Width = 54, Height = 72, Unit = MeasurementUnit.Centimeter, SortOrder = 3 });
            product.Sizes.Add(new ProductSize { Code = "XL", Name = "X-Large", Width = 58, Height = 74, Unit = MeasurementUnit.Centimeter, SortOrder = 4 });
            product.Sizes.Add(new ProductSize { Code = "XXL", Name = "XX-Large", Width = 62, Height = 76, Unit = MeasurementUnit.Centimeter, SortOrder = 5 });

            product.PrintingOptions.Add(new PrintingOption
            {
                Name = "Front only",
                Code = "front_only",
                Price = 40,
                IncludedSurfaceCodes = new List<string> { "front" },
                Status = EntityStatus.Active,
                SortOrder = 1
            });
            product.PrintingOptions.Add(new PrintingOption
            {
                Name = "Front + Back",
                Code = "front_back",
                Price = 70,
                IncludedSurfaceCodes = new List<string> { "front", "back" },
                Status = EntityStatus.Active,
                SortOrder = 2
            });

            product.EmbroideryPricing = new EmbroideryPricing
            {
                PricePerSquareUnit = 0.85m,
                Unit = MeasurementUnit.Centimeter,
                MinimumCharge = 20,
                Status = EntityStatus.Active
            };

            db.Products.Add(product);

            db.DesignAssetCategories.Add(new DesignAssetCategory
            {
                Name = "Symbols",
                Status = EntityStatus.Active,
                Assets =
                {
                    new DesignAsset
                    {
                        Name = "Star",
                        FileUrl = "/assets/symbols/star.png",
                        Status = EntityStatus.Active,
                        Tags = new List<string> { "star", "symbol" }
                    }
                }
            });

            logger.LogInformation("Seeded classic t-shirt product configuration for zouq");
        }

        await db.SaveChangesAsync();
    }
}
