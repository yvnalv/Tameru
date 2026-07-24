# Frontend Architecture

The Tameru web client. Mobile-first, dark, built to the [DESIGN_LANGUAGE.md](DESIGN_LANGUAGE.md).

## Stack

- **Vue 3** (`<script setup>`, Composition API) + **TypeScript** (strict)
- **Vite** (dev server + build)
- **Pinia** (state), **Vue Router 4** (routing)
- **Tailwind CSS** (utilities) driven by **CSS custom properties** for theming
- **vue-i18n** (English + Bahasa Indonesia)
- **Apache ECharts** via `vue-echarts`
- **axios** (API client), **Lucide** (icons)
- **Vitest** (unit tests)

Lives in **`frontend/`** as its own project (sibling to `src/`).

## Structure

```
frontend/src/
├── assets/styles/   tokens.css (design tokens, dark) + main.css (Tailwind + base)
├── components/
│   ├── layout/      AppSidebar, AppTopbar, MobileNav (bottom pill)
│   └── ui/          AppButton, AppCard, BalanceCard, StatTile, StatusChip,
│                    TransactionRow, SpendBar, Money, DataTable, FormField, AppInput, AppSelect
├── i18n/            index.ts + locales/{en.ts, id.ts}
├── layouts/         AppShell (responsive: sidebar+topbar on desktop, bottom-nav on mobile)
├── lib/             api.ts (axios + envelope unwrap + 401 handling), format.ts (id-ID money/number),
│                    accounts.ts, ledger.ts, budgeting.ts, reports.ts (typed API modules)
├── router/          routes + auth guard
├── stores/          auth, theme, ui (density/locale)
├── types/           api.ts (envelope + DTOs)
└── views/           LoginView, DashboardView, transactions/, accounts/, budget/, masterPlan/, categories/
```

## Views ↔ menu

`Dashboard` · `Transactions` (Income/Expense/Transfer list + detail + create) · `Accounts` (list +
detail with monthly balances) · `Budget` (period Plan/Actual/Leftover) · `Master Plan`
(Investment/Needs/Wants) · `Categories` (taxonomy tree). Later phases add their own view folders.

## Theming

`tokens.css` defines all colors as CSS variables on `:root` (dark). Tailwind's theme maps utilities
to those variables (`tailwind.config.ts`). A future light theme flips `data-theme` on `<html>`.
ECharts reads the CSS vars at runtime (solid category-spectrum series, dark tooltip). **No gradient
fills anywhere.** See [DESIGN_LANGUAGE.md](DESIGN_LANGUAGE.md).

## API & auth

- `lib/api.ts` — axios at `/api/v1`; request interceptor attaches the bearer token; `unwrap` peels
  the `{ success, data }` envelope; a 401 attempts one refresh, else clears the session → `/login`.
- `stores/auth.ts` — login (`POST /auth/login`), refresh rotation, session (token + user in
  `localStorage`), current locale.
- Router guard redirects unauthenticated users to `/login` (with `?redirect=`).

## i18n

- Two structurally-identical dictionaries `locales/en.ts` and `locales/id.ts`. Every user-facing
  string goes through `t('…')`; enums (transaction type, status, category level) translate via a map
  keyed on the enum value. Seeded reference names (starter categories/account groups) use a
  code→{en,id} map that overrides only while still at the seeded default. Live language switching.

## Conventions

- Money/qty via `formatMoney`/`formatNumber` (id-ID, negatives in parentheses) + the `.tnum`
  (tabular figures) class. Never format numbers inline.
- Reusable presentation in `components/ui`; one accent (green) used sparingly; semantic colors for
  status; Lucide icons; no emoji; **no gradients**.
- API access only through typed `lib/*` modules; components never call `axios` directly.

## Build

```bash
npm install
npm run dev        # http://localhost:5173  (proxies /api → http://localhost:5080)
npm run build      # vue-tsc typecheck + vite production build
npm run test       # vitest
```
