# Phase 11 — Published Design Reuse Fix

**Date:** 2026-09-18  
**Scope:** Published-design reuse only (no catalog/AI/ads/perf work)

---

## 1. What changed

Fixed the critical bug where ordering a design mutated the **same** row into `Ordered` + `Private`, which broke reuse of published designs.

Implemented:

- `Design.DerivedFromDesignId` attribution
- `POST /api/designs/{id}/derive` — clone published → new private draft
- Order authorization (own draft only; published must be derived first)
- Creator resolution: derived drafts credit **source owner** via `OrderItem.DesignerId`
- Source published design **never** mutated on reuse
- Minimal Flutter: For You → **Use Design** → editor with derived draft → order
- Tests: `DesignReuseTests` (+ existing financial suite still green)
- Migration: `DesignDerivedFrom`

---

## 2. Architecture used

```
Source Design (User A)
  Status=Reusable, Visibility=Public, Owner=A
        │
        │  POST /api/designs/{id}/derive
        ▼
Derived Draft (User B)
  Owner=B, Status=Draft, DerivedFromDesignId=Source.Id
        │
        │  POST /api/orders  (buyer owns draft)
        ▼
Order
  BuyerId=B
  OrderItem.SourceDesignId = Derived.Id   (buyer draft)
  OrderItem.DesignerId     = A            (source owner)
  Snapshots of B’s final config
        │
        │  Delivered
        ▼
Existing FinancialLedgerService credits DesignerId (= A)
```

---

## 3. Source vs derived

| | Source published | Derived draft |
|--|------------------|---------------|
| Owner | Original creator (A) | Second customer (B) |
| Status after reuse | **Reusable** (unchanged) | Ordered → later DeliveredEligible |
| Visibility | **Public** (unchanged) | Private |
| Mutated on B’s order? | **No** | Yes (becomes Ordered) |

---

## 4. Buyer vs creator

- **Buyer** = authenticated order caller (`Order.BuyerId`)
- **Creator / Designer** = `ResolveCreatorAsync`:
  - If `DerivedFromDesignId` set → source design’s `OwnerId`
  - Else → draft’s `OwnerId`
- Self-orders (buyer == designer) still skip reward (existing ledger rule)

---

## 5. Authorization rules

**Order (`CreateFromDesignAsync`):**

- Allowed: caller owns design AND status is editable Draft/Unpublished
- Rejected (403): another user’s private design
- Rejected (403): ordering a Reusable/Public design **directly** — must derive first

**Derive (`DeriveAsync`):**

- Source must exist, not deleted
- `Status == Reusable` AND `Visibility == Public`
- Related `SourceOrder` must be Delivered (if linked)
- Refunded/unpublished sources fail (status no longer Reusable)

All checks are server-side.

---

## 6. API changes

| Method | Route | Purpose |
|--------|-------|---------|
| POST | `/api/designs/{id}/derive` | Clone published → private draft for current user |
| (existing) | `/api/orders` | Orders owned drafts only; creator from derivation |

`UnauthorizedAccessException` → **HTTP 403** (`ApiExceptionFilter`).

`DesignDto` now includes `derived_from_design_id`.

---

## 7. Database changes

- Column: `Designs.DerivedFromDesignId` (nullable FK → `Designs`, `Restrict`)
- Index: `IX_Designs_DerivedFromDesignId`
- Migration: `Zouq.Infrastructure/Data/Migrations/..._DesignDerivedFrom.cs`
- Seeder continues to use `MigrateAsync` (no EnsureCreated)

Apply: `dotnet ef database update --project Zouq.Infrastructure --startup-project Zouq.Api`

---

## 8. Financial impact

- **No rewrite** of Delivered reward / ledger / concurrency / refund
- Only correct `OrderItem.DesignerId` so reuse orders credit User A
- No reward at order creation (unchanged)
- Existing financial tests: **PASS**

---

## 9. Flutter changes

- Feed card: **Use Design** → `POST derive` → `/products/{productId}/design?draftId=…`
- Editor loads derived draft (config + elements)
- Save/create order uses PUT when draft already exists (preserves `DerivedFromDesignId` server-side)

---

## 10. Tests added

`DesignReuseTests` covers:

1. Owner orders own draft  
2. Cannot order others’ private draft  
3. Cannot order published directly (must derive)  
4–7. Derive does not mutate source (Reusable/Public)  
8–9. Derived owned by second user + `DerivedFromDesignId`  
10–13. Order buyer/creator + no reward at create + reward to A on Delivered  
14–15. Two/three independent reuse orders → independent rewards  
16. Concurrent Delivered idempotent on reuse order  
17. Private/undelivered/unpublished cannot derive  
18. Refunded source cannot derive  
19. Modifying derived does not modify source  

---

## 11. Existing tests result

```
dotnet test Zouq.Tests/Zouq.Tests.csproj
Passed: 37  Failed: 0  Skipped: 0
```

---

## 12. Build / analyze results

| Command | Result |
|---------|--------|
| `dotnet test Zouq.Tests` | 37 passed |
| `dotnet build Zouq.Api` | 0 errors |
| `flutter analyze` | No issues |
| `npm run build` (admin) | success |

---

## 13. Remaining limitations

- Direct order of a published design is intentionally forbidden (derive required); Flutter implements Use Design → derive.
- Derived drafts that User B later publishes become their own reusable designs (separate from A’s source) — expected.
- No polished reuse UX (quantity, preview, deep-link ads) beyond minimal Use Design → editor → order.
- Existing DBs need the new migration applied.

---

## Explicit verification checklist

| Invariant | Status |
|-----------|--------|
| SOURCE published design remains public/reusable after reuse | **PASS** (tests) |
| SECOND USER gets independent derived design/order | **PASS** |
| ORIGINAL OWNER remains Creator (`DesignerId`) | **PASS** |
| CREATOR REWARD only after Delivered | **PASS** |
| NO SOURCE DESIGN MUTATION | **PASS** |
| NO PRIVATE DESIGN LEAK (order/derive auth) | **PASS** |
