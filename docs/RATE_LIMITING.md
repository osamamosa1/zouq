# Rate limiting

ASP.NET Core rate limiter is enabled in `Program.cs` via `UseRateLimiter`.

| Policy | Applied to | Limit |
|--------|------------|-------|
| `auth` | `/api/auth/*` | 20 req / min / IP |
| `upload` | `/api/uploads/*` | 30 req / min / user or IP |
| `mutate` | `/api/designs/*`, `/api/orders/*` | 60 req / min / user or IP |
| `feed` | `/api/feed/*`, `/api/assets/*` | 120 req / min / IP |

Exceeded requests return **429 Too Many Requests**.

Tune for production traffic without making normal mobile usage painful.
