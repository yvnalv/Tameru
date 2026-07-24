# Tameru — Frontend

Vue 3 + TypeScript + Vite + Tailwind web client. Dark-first, single green accent, bilingual (EN + ID).
Design and structure: [../docs/frontend/](../docs/frontend/).

## Quick start (local dev)

```bash
npm install
npm run dev      # http://localhost:5173
```

The dev server proxies `/api` to the backend. It defaults to the local Docker API
(`http://localhost:8090`, from the repo-root `docker-compose.yml`). Point it elsewhere — e.g. the
API run via `dotnet run` — with:

```bash
VITE_API_PROXY=http://localhost:5080 npm run dev
```

## Scripts

| Script | What it does |
|---|---|
| `npm run dev` | Vite dev server with HMR |
| `npm run build` | `vue-tsc` typecheck + production build to `dist/` |
| `npm run test` | Vitest unit tests |
| `npm run preview` | Serve the production build locally |

## Full stack on Docker

From the repo root, `docker compose up -d --build` runs `web` (this SPA via Nginx) → `api` → `db`.
The app is then at **http://localhost:8091**; Nginx proxies same-origin `/api` to the API container.
Seeded owner login: `owner@tameru.local` / `ChangeMe!123`.

## Conventions

- Money/number formatting only through `lib/format.ts` (id-ID, negatives in parentheses) + the
  `.tnum` class. Never format numbers inline.
- API access only through typed `lib/*` modules; components never call axios directly.
- All user-facing text goes through `t('…')`; the `en` and `id` dictionaries stay structurally
  identical. One green accent, semantic colors for status, Lucide icons, no emoji, no gradients.
