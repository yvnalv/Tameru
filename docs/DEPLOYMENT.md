# Deployment

Tameru ships as a small Docker stack: the ASP.NET Core API, PostgreSQL, and the built Vue SPA served
by Nginx (which also reverse-proxies `/api`).

## Environments

| Environment | Purpose | Config source |
|---|---|---|
| **Local** | Developer machine | `appsettings.Development.json` + user-secrets / `.env` |
| **Development** | Shared dev/test | env vars + compose overrides |
| **Production** | Live single-user app | env vars / secrets store |

Configuration precedence: `appsettings.json` → `appsettings.{Environment}.json` → environment
variables. **Secrets only via environment variables / user-secrets** — never committed.

## Local run

```bash
# Backend (from repo root)
cd src/Bootstrapper/Tameru.Api
ASPNETCORE_ENVIRONMENT=Development Database__Initialize=true Database__AutoMigrate=true Seed__Enabled=true dotnet run
# → http://localhost:5080  (Swagger at /swagger)

# Frontend
cd frontend
npm install
npm run dev        # http://localhost:5173  (proxies /api → http://localhost:5080)
```

Dev owner login is created by the seed (email/password from configuration; change immediately).

## Docker

- `Dockerfile.api` — multi-stage .NET build → runtime image.
- `frontend/Dockerfile` — build the SPA → serve via Nginx with `nginx.conf` (SPA fallback + `/api`
  proxy).
- `docker-compose.yml` — production-like: `api`, `db` (Postgres with a named volume), `web` (Nginx).
- `docker-compose.dev.yml` — dev conveniences (hot config, exposed ports).

```bash
docker compose up -d --build
```

## Database

- Migrations are per-module EF Core migrations. In Development they auto-apply
  (`Database__AutoMigrate=true`); in Production apply explicitly during deploy
  (`dotnet ef database update` or a migration bundle) before the new API starts.
- Postgres is **not** publicly exposed — bind to loopback and reach it via SSH tunnel for admin.
- **Backups:** scheduled `pg_dump` to an off-box location; the DB is the system of record.

## CI/CD (GitHub Actions)

1. Build backend + run unit/architecture tests (integration when Docker available).
2. Build frontend + run Vitest.
3. On main: build and push images; deploy (compose pull + up) to the target host; run migrations.

## Configuration keys (indicative)

| Key | Meaning |
|---|---|
| `ConnectionStrings__Postgres` | DB connection (secret) |
| `Jwt__SigningKey` | JWT signing secret |
| `Jwt__AccessMinutes` / `Jwt__RefreshDays` | token lifetimes |
| `Database__AutoMigrate` / `Database__Initialize` | dev startup behavior |
| `Seed__Enabled` | seed owner + reference data |
| `Cors__AllowedOrigins` | SPA origin(s) |

## Rollback

Roll back by redeploying the previous image tag. Never edit historical migrations; add a corrective
migration instead. Record notable deploys/rollbacks in `CHANGELOG.md`.
