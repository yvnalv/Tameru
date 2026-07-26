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
# 1) Dev database (Postgres in Docker, loopback-only on host port 5433)
docker compose -f docker-compose.dev.yml up -d

# 2) Backend (from repo root)
cd src/Bootstrapper/Tameru.Api
dotnet run        # ASPNETCORE_ENVIRONMENT=Development via launchSettings
# → http://localhost:5080  (Swagger at /swagger). On startup it auto-migrates and seeds the owner.

# 3) Frontend (later milestone)
cd frontend
npm install
npm run dev        # http://localhost:5173  (proxies /api → http://localhost:5080)
```

`appsettings.Development.json` points at the dev DB (`Host=localhost;Port=5433;…`), enables
`Database:AutoMigrate` and `Seed:Enabled`, and carries a dev-only `Jwt:SigningKey`.

**Dev owner login (seeded, change for anything real):** `owner@tameru.local` / `ChangeMe!123`.
Configure via the `Seed:Owner` section or environment variables.

## Docker

- `Dockerfile.api` — multi-stage .NET build → runtime image.
- `frontend/Dockerfile` — build the SPA → serve via Nginx with `nginx.conf` (SPA fallback + `/api`
  proxy).
- `docker-compose.yml` — production-like: `api`, `db` (Postgres with a named volume), `web` (Nginx).
- `docker-compose.dev.yml` — dev conveniences (hot config, exposed ports).

```bash
docker compose up -d --build
```

## Deploy to a VPS

The whole app ships as one Docker Compose stack (`web` → `api` → `db`). On a fresh server with Docker
installed:

```bash
git clone https://github.com/yvnalv/Tameru.git && cd Tameru
cp .env.example .env
# Edit .env: set ASPNETCORE_ENVIRONMENT=Production, a strong POSTGRES_PASSWORD and Jwt__SigningKey,
# the SEED_OWNER_* account, and CORS_ORIGIN=https://your-domain.
docker compose up -d --build
```

- On first start the API auto-migrates every module and seeds the owner from `SEED_OWNER_*`.
- The SPA (`web`) serves on `WEB_PORT` (default 8091) and proxies `/api` to the API over the internal
  network — **only `web` needs to be public.** Keep `api` (8090) and `db` closed on the firewall, or
  remove the `api` host port mapping entirely in production.
- **TLS / domain:** put a reverse proxy (Caddy, Traefik, or Nginx) in front of `web` to terminate
  HTTPS for your domain and forward to `WEB_PORT`. Caddy example: `your-domain { reverse_proxy
  localhost:8091 }`.
- **Change the seeded owner password** after first login (the seed only runs once).
- **Updates:** `git pull && docker compose up -d --build`. Migrations apply automatically on API start.

### On a shared multi-app VPS (GHCR images + shared Nginx + shared Postgres)

When Tameru runs next to other apps behind one Nginx and one Postgres, don't build on the server —
CI publishes the images to GHCR and you pull them. Add two services to your existing `docker-compose.yml`:

```yaml
  # ── Tameru API (.NET 8) ──
  tameru-api:
    image: ghcr.io/yvnalv/tameru-api:${TAMERU_TAG:-latest}
    container_name: tameru-api
    restart: unless-stopped
    mem_limit: 512m
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      # Uses the shared Postgres. EF creates the "tameru" database on first run if missing.
      ConnectionStrings__Postgres: "Host=postgres;Port=5432;Database=${TAMERU_DB:-tameru};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}"
      Jwt__SigningKey: ${TAMERU_JWT_SIGNING_KEY:?set TAMERU_JWT_SIGNING_KEY in .env (>=32 chars)}
      Jwt__Issuer: Tameru
      Jwt__Audience: Tameru
      Database__AutoMigrate: "true"
      Seed__Enabled: "true"
      Seed__Owner__Email: ${TAMERU_OWNER_EMAIL:?set TAMERU_OWNER_EMAIL in .env}
      Seed__Owner__Password: ${TAMERU_OWNER_PASSWORD:?set TAMERU_OWNER_PASSWORD in .env}
      Seed__Owner__DisplayName: ${TAMERU_OWNER_NAME:-Yovan}
      Seed__Owner__Locale: en
      Cors__AllowedOrigins__0: ${TAMERU_ORIGIN:-https://tameru.yvnalvworks.com}
    expose:
      - "8080"
    depends_on:
      postgres:
        condition: service_healthy

  # ── Tameru SPA (Vue + Nginx; proxies /api → tameru-api) ──
  tameru-web:
    image: ghcr.io/yvnalv/tameru-web:${TAMERU_TAG:-latest}
    container_name: tameru-web
    restart: unless-stopped
    mem_limit: 64m
    expose:
      - "80"
    depends_on:
      - tameru-api
```

Add a server block to your shared Nginx config for the subdomain (front `tameru-web`; the SPA image
already proxies `/api` internally to `tameru-api`):

```nginx
server {
    listen 443 ssl;
    server_name tameru.yvnalvworks.com;
    ssl_certificate     /etc/letsencrypt/live/tameru.yvnalvworks.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/tameru.yvnalvworks.com/privkey.pem;
    location / {
        proxy_pass http://tameru-web:80;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

`.env` additions:

```dotenv
TAMERU_TAG=latest
TAMERU_DB=tameru
TAMERU_JWT_SIGNING_KEY=<openssl rand -base64 48>
TAMERU_OWNER_EMAIL=you@example.com
TAMERU_OWNER_PASSWORD=<strong>
TAMERU_OWNER_NAME=Yovan
TAMERU_ORIGIN=https://tameru.yvnalvworks.com
```

Steps:

1. **Publish images** — merge to `main` so CI pushes `ghcr.io/yvnalv/tameru-api` and `tameru-web`.
2. **DNS** — add an A record `tameru.yvnalvworks.com` → the VPS IP; verify with
   `dig +short tameru.yvnalvworks.com`.
3. **TLS cert** — the Nginx container mounts `/etc/letsencrypt` read-only, so a host certbot cert is
   picked up automatically. The zero-config method (brief Nginx stop while certbot binds port 80):
   ```bash
   docker compose stop nginx
   sudo certbot certonly --standalone -d tameru.yvnalvworks.com \
     --non-interactive --agree-tos -m you@yvnalvworks.com
   docker compose start nginx
   ```
   This writes `/etc/letsencrypt/live/tameru.yvnalvworks.com/{fullchain,privkey}.pem`. (Renewal:
   `certbot renew` runs from certbot's systemd timer; add a deploy hook to reload the container —
   `certbot renew --deploy-hook "docker compose -f /path/docker-compose.yml restart nginx"`.)
4. **Add the services + Nginx block**, fill `.env`, then:
   ```bash
   docker compose pull tameru-api tameru-web
   docker compose up -d
   docker compose exec nginx nginx -t && docker compose restart nginx
   ```

The API auto-creates/migrates the `tameru` DB and seeds the owner. Change the owner password after
first login. Update later with `docker compose pull tameru-api tameru-web && docker compose up -d`.

## Database

- Migrations are per-module EF Core migrations. In Development they auto-apply
  (`Database__AutoMigrate=true`); in Production apply explicitly during deploy
  (`dotnet ef database update` or a migration bundle) before the new API starts.
- Postgres is **not** publicly exposed — bind to loopback and reach it via SSH tunnel for admin.
- **Backups:** scheduled `pg_dump` to an off-box location; the DB is the system of record.

## CI/CD (GitHub Actions)

`.github/workflows/ci.yml` runs on every push/PR to `main`:

1. **Backend** — `dotnet restore/build/test Tameru.slnx` (unit + architecture + **integration** tests;
   the integration tests spin up PostgreSQL via Testcontainers on the runner's Docker).
2. **Frontend** — `npm ci`, `npm run build` (`vue-tsc` typecheck + Vite build), `npm run test` (Vitest).

Deploy is manual for now (`git pull && docker compose up -d --build` on the VPS); an image-push +
remote-deploy step can be added later.

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
