# Zouq architecture

## Reused from the old project (technical only)

- Clean Architecture layers: Domain / Application / Infrastructure / Api
- JWT bearer auth + refresh tokens + BCrypt
- Snake_case JSON API responses
- EF Core + SQL Server
- Flutter feature folders: data / domain / presentation + Bloc + Dio + GetIt

## Not reused

- Any education / courses / exams / teachers domain
- Hardcoded product business rules from the old app

## Dynamic product model

```
ProductType
  └── Product
        ├── ProductSurface (front, back, sleeve, …)
        │     └── ProductDesignArea (norm rect + real cm)
        ├── ProductFabric → Fabric
        ├── ProductCut → CutStyle
        ├── ProductSize (width/height in cm — source of truth)
        ├── PrintingOption (named package price + included surfaces)
        └── EmbroideryPricing (price per cm²)
```

Adding Hoodie later = new Product rows + surfaces/sizes/options in admin. No backend redesign.

## Design editor contract

Client sends elements with:

- `norm_x|y|width|height` relative to the surface design area (0..1)
- `real_width|height` = norm × design-area real dimensions (same unit as product)
- `production_method`: Printing | Embroidery
- `surface_code`

Backend recalculates price on save and again on order; client estimates only.

## Order immutability

`OrderItem` stores:

- `design_snapshot_json`
- `pricing_breakdown_json`
- `product_snapshot_json`

Catalog price edits never mutate past orders.
