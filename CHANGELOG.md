# Tameru Changelog

This file is Tameru's immutable historical record. A task is not complete until this file has been
updated. Newest entries at the top. See `CLAUDE.md` → **CHANGELOG Rules** for the full procedure.

## [2026-07-26 03:58:27 UTC]

CHG-0028 — Responsive: field overflow + compact tables on small screens

- **Field overflow fixed.** `AppInput`, `AppSelect`, and `FormField` now set `min-w-0`, so date/number
  inputs shrink within their grid cell instead of forcing horizontal overflow on phones.
- **Modal forms stack on phones.** The paired-field grids in the Account, Transaction, and Master Plan
  modals are now `grid-cols-1 sm:grid-cols-2` — one column (full-width fields) under ~640px, two above.
- **Compact Master Plan table.** The Price and Frequency columns are hidden below `sm` (Total =
  Price × Frequency remains), and the table drops its min-width on phones so it fits without a
  horizontal scroll.
- `vue-tsc` + build green; 19 Vitest tests; Docker `web` rebuilt.

## [2026-07-25 14:14:35 UTC]

CHG-0027 — M9 part 9: consistent money format + logged-in user in the top bar

- **One money format everywhere.** Dropped the `+`/`−` signed style (`formatSignedMoney`); every amount
  now uses the id-ID **parentheses** style for negatives (per the locked design language). The `Money`
  component always renders **negatives in red**; positives stay neutral unless `colored` marks a
  semantic gain (income / net / leftover), which renders green. Transaction rows now show income green,
  expense `(Rp …)` red, transfer neutral — matching the rest of the app. Removed the unused
  `TransactionRow` component and the signed-format unit tests.
- **Logged-in owner** shown in the top bar: an avatar chip with the display name + email (avatar-only
  on mobile). The sign-out button became a tooltip'd icon button.
- `vue-tsc` + build green; 19 Vitest tests; Docker `web` rebuilt.

## [2026-07-25 14:06:02 UTC]

CHG-0026 — M9 part 8: cross-cutting polish (toasts, confirm dialog, skeletons, self-hosted Inter)

- **Toasts**: a `toast` store + `ToastHost` (teleported, bottom, auto-dismiss, success/error/info with
  icons). All former `window.alert(...)` error popups now raise `toast.error(...)`, and destructive
  successes show a `toast.success(...)`.
- **Confirm dialog**: a promise-based `confirm` store + `ConfirmDialog` (styled, Esc/backdrop to
  cancel, danger variant) replaces every `window.confirm(...)` — deactivate account/category, void
  transaction, delete master-plan item. Both hosts are mounted once in `AppShell`.
- **Loading skeletons**: `Skeleton` + `LoadingBlock` (shimmer, respects reduced-motion) replace the
  "Loading…" text across Dashboard, Accounts, Transactions, Categories, Budget, Master Plan, and a
  lighter row skeleton in Reports.
- **Self-hosted Inter** via `@fontsource/inter` (400/500/600/700 woff2, bundled — 28 font assets); the
  app no longer falls back to a system font, sharpening all type incl. the wordmark.
- Added `common.confirm` / `common.done` to both locales. `vue-tsc` + build green (main bundle
  ~225 kB; ECharts stays in the lazy Dashboard chunk); 22 Vitest tests; Docker `web` rebuilt.

This completes **M9 — UI/UX hardening** (CHG-0019…0026).

## [2026-07-25 13:54:47 UTC]

CHG-0025 — M9 part 7: richer dashboard with ECharts

- Adopted **Apache ECharts** via `vue-echarts` (the documented stack), tree-shaken to only the chart
  types/components used (`BarChart`, `PieChart`, grid, tooltip, canvas renderer) and **code-split into
  the lazy Dashboard chunk** so the main bundle stays ~218 kB. Chart colors mirror the design tokens
  (`lib/chartTheme.ts`), dark tooltip included.
- Rebuilt the **Dashboard** into a richer layout:
  - Net-worth hero (with the account spend-bar) + a "This month" income/expense/net card.
  - **Cashflow** — a 12-month income vs. expense bar chart (ECharts, solid green/red, no gradient),
    replacing the CSS bars.
  - **Expenses by category** — a donut of this month's spend (top 6 + "Others") with a colored legend
    and a centered total.
  - **Recent transactions** — the latest 10 as an activity feed (avatar, date · category, signed
    amount), plus the accounts summary.
- Added `DonutChart` component and dashboard i18n (`expenses`, `others`, `noExpenses`) in both locales.
- `vue-tsc` + build green; 22 Vitest tests; Docker `web` rebuilt. Verified dashboard data end-to-end
  (5-category donut for the month; 10 recent transactions).

## [2026-07-25 13:45:49 UTC]

CHG-0024 — M9 part 6: progress-bar fix + import on every screen

- **Budget progress bar** fix: the green (up-to-plan) segment had its own `rounded-full`, which
  rounded the green→red junction while the red segment stayed square — mismatched. The segments are
  now plain rectangles inside the rounded, clipped container, so the junction is a clean straight edge
  and only the outer ends are rounded.
- **Import everywhere**: added CSV importers for **Categories** (name / level / parent / flow —
  parents resolved against existing categories) and **Master Plan** items (section / name / price /
  frequency — section resolved by name). Import is now on Accounts, Transactions, Categories, and
  Master Plan, each reusing the preview→import→report `ImportModal` and the existing validated
  create endpoints. Added `import.categories` / `import.masterPlan` to both locales.
- `vue-tsc` + build green; 22 Vitest tests; verified both new importers create records against the
  live API (a Category under an existing budget; a Master Plan item 2,000,000 × 12 = 24,000,000).

## [2026-07-25 13:36:27 UTC]

CHG-0023 — M9 part 5: Reports monthly year nav + Budget progress bars

- **Reports → Monthly** now has ‹ prev / next › **year** navigation (was fixed to the current year);
  Daily keeps its month nav, Yearly stays a fixed last-5-years range.
- **Budget** view now renders each category as a **progress bar** instead of a plain table row: a green
  fill up to the Plan and a **red segment for the overspend** beyond it, with `Actual / Plan`, the used
  **%** (red when over 100%), and the per-line leftover. The totals cards and the "Edit plans" mode are
  unchanged.
- `vue-tsc` + build green; 22 Vitest tests; Docker `web` rebuilt.

## [2026-07-25 13:28:51 UTC]

CHG-0022 — M9 part 4: Reports — single card with Yearly/Monthly/Daily toggle

- Merged the two Reports cards (yearly overview + category tracker — they showed the same data) into
  **one** "Category tracker" card with a `Yearly | Monthly | Daily` toggle:
  - **Yearly** — the last 5 years (columns = years), aggregated **client-side** from monthly data.
  - **Monthly** — the 12 months of the current year.
  - **Daily** — the days of the selected month, with ‹ prev / next › month navigation (like Budget).
- Kept the heatmap cells, sticky category column, period + grand totals, and the top-categories
  summary; all three modes share one reshaping helper over the `category-tracker` endpoint.
- Added `reports.yearly` to both locales. (A dedicated backend yearly-aggregation endpoint is a noted
  future improvement — see docs/API_SPEC.md — for after the UI stabilizes.)
- `vue-tsc` + build green; 22 Vitest tests; verified data for all three modes.

## [2026-07-25 13:24:00 UTC]

CHG-0021 — M9 part 3: content area fills the screen width

- The main content was capped at `max-w-[1600px]` and left-aligned, leaving dead space on wide
  screens (and it didn't reclaim the space when the sidebar collapsed). Removed the cap so the content
  region is fluid — it fills the available width and expands/contracts with the sidebar. Committed with
  the sidebar-toggle fix in the same PR.

## [2026-07-25 13:18:52 UTC]

CHG-0020 — M9 part 2: fixed sidebar toggle + responsive/mobile pass

- **Sidebar toggle moved to a fixed spot in the top bar** (far left, before the page title). It no
  longer jumps between the sidebar header and a floating button when collapsing — it stays in one
  place and only its icon flips (a well-established pattern). The sidebar header now shows just the
  logo (lockup when open, mark when collapsed).
- **Responsive/mobile pass** targeting ~375px (iPhone 8) and Android:
  - Page headers wrap their action buttons (Transactions, Accounts) instead of overflowing.
  - Mobile-friendly transaction rows: the status chip is hidden on phones (status shown inline in the
    meta line), the amount no longer has a fixed width, tighter padding — no horizontal overflow.
  - Budget totals stack (1-col) on phones; Master Plan section headers wrap and the "Add item" label
    collapses to an icon; the big BalanceCard number scales down (`text-3xl` on mobile) and wraps.
- `vue-tsc` + build green; 22 Vitest tests; Docker `web` rebuilt.

## [2026-07-25 13:09:51 UTC]

CHG-0019 — M9 (UI/UX hardening) part 1: quick wins

- **Month labels** now use the short format (`Jan`, `Feb`, …) instead of single letters, in the
  cashflow chart and the Reports matrices.
- New reusable **`IconButton`** — an icon-only button that is always labelled (a styled tooltip on
  hover/focus + `aria-label`). Applied to every icon-only action (transaction rows, accounts,
  categories, master-plan, the topbar density toggle). The transaction-row actions now read clearly
  (mark cleared / mark uncleared / void) with clearer icons.
- **Collapsible sidebar**: a toggle collapses it to a 72px icon-only rail (labels shown as tooltips);
  the state persists (`tameru.sidebarCollapsed`). Uses the brand mark when collapsed, lockup when open.
- **Scrollable tables**: the Budget and Master Plan tables now scroll horizontally within their own
  container (Reports already did), so nothing overflows the page on narrow screens.
- Added `common.collapse`/`common.expand` to both locales (i18n parity test still green).
- `vue-tsc` + build green; 22 Vitest tests; Docker `web` rebuilt.

## [2026-07-25 12:29:46 UTC]

CHG-0018 — M8 (part 3): CSV import (accounts & transactions)

- Client-side CSV import — no backend change; it reuses the existing, fully validated create
  endpoints (one request per row), so all money rules still apply server-side.
- `lib/csvParse.ts`: an RFC-4180 parser (quoted fields, escaped quotes, embedded commas/newlines,
  CRLF, BOM) with 5 unit tests. `lib/import.ts` (config type + template download + lenient amount
  parse) and `lib/importConfigs.ts` (accounts + transactions configs resolving account/category names
  → ids, with an early flow-mismatch guard).
- `ImportModal`: upload → **preview** (per-row valid/skip with reasons) → **import** (progress bar) →
  **report** (created / failed with messages); a "Download template" button emits a headers+sample CSV.
- Wired an **Import** action into the Transactions and Accounts screens.
- i18n `import.*` in EN + ID (kept structurally identical).
- `vue-tsc` + build green; frontend suite now 22 Vitest tests. Docker `web` rebuilt. Verified the flow
  end-to-end against the live API (a valid Expense + Transfer created; a row with an unknown account
  skipped, matching the preview's validation).

- **i18n parity test** (`src/i18n/locales.spec.ts`): asserts the EN and ID dictionaries have an
  identical key set and no empty values — a permanent guard against locale drift (CLAUDE.md i18n rule).
- **Density toggle**: `useDensity` composable over the ui store's persisted `density`; a topbar control
  (Rows2/Rows3 icon) switches comfortable/compact, applied to the Transactions and Accounts list rows.
- **CSV export** of transactions: `lib/csv.ts` (RFC-4180 quoting, UTF-8 BOM for Excel) + an Export
  button on the Transactions screen that pulls all rows matching the active filters and downloads
  `tameru-transactions-YYYY-MM-DD.csv` with localized headers.
- Added `common.density` / `common.export` and `transactions.type` to both locales (kept identical).
- `vue-tsc` + build green; frontend suite now 17 Vitest tests; Docker `web` rebuilt.

- `Program.cs` uses `Tameru.Reporting.Api`/`Tameru.Reporting.Infrastructure`, but the
  `Tameru.Reporting.Api` `<ProjectReference>` had been dropped from `Tameru.Api.csproj`, so the API no
  longer compiled (`CS0234: … 'Reporting' does not exist in the namespace 'Tameru'`). Restored the
  reference. The regression went unnoticed because only the frontend `web` image had been rebuilt
  since M5; the `api` image ran a stale build until a full `docker compose up --build`.
- `dotnet build` clean (0 warnings/errors); the API image builds and boots again.

## [2026-07-25 12:04:11 UTC]

CHG-0015 — M8 (part 1): Reports / Analytics screen

- New **Reports** screen (`/reports`, added to the menu with a chart icon) surfacing the M5 analytics
  endpoints that no screen used yet — no backend change:
  - **Yearly overview**: a category × 12-month spending matrix with heatmap cells (accent-intensity by
    value), a sticky category column, month/grand totals, and a top-categories summary (SpendBar +
    list). Year prev/next selector.
  - **Category tracker**: a category × period pivot with a Monthly/Daily granularity toggle and a
    from/to date range; sticky category column, period + grand totals.
- Typed `getOverview` / `getCategoryTracker` in `lib/reports.ts` (+ Overview/CategoryTracker types);
  category ids resolved to localized names via `seededNames`. Compact id-ID cell formatting; both
  tables scroll horizontally within their own container (no page overflow).
- i18n: `nav.reports` + a `reports.*` block in EN and ID (kept structurally identical).
- `vue-tsc` + build + 15 Vitest tests green; Docker `web` rebuilt. Verified end-to-end (overview 5
  categories / total 63,246,000; monthly tracker 7 periods × 5 categories; `/reports` history
  fallback 200).

## [2026-07-24 17:07:01 UTC]

CHG-0014 — M7 (part 2): Categories, Budget & Master Plan screens — MVP feature-complete

- Typed `lib/budgeting.ts` (budget periods + lines, master plan) and budgeting types; a
  `seededNames` helper that localizes seeded reference names (categories, master-plan sections) only
  while still at their English default (a user rename shows verbatim, per CLAUDE.md i18n rules).
- **Categories screen** (`/categories`): the Budget→Category→Sub tree grouped by budget with flow +
  system badges; add budget / add child / add sub, edit, and deactivate (system-guarded).
- **Budget screen** (`/budget`): month prev/next picker; Plan/Actual/Leftover totals and a per-line
  table (Actual/Leftover derived from the ledger); "Edit plans" mode upserts plan amounts per expense
  category; create-period flow when a month has none.
- **Master Plan screen** (`/master-plan`): Investment/Needs/Wants sections with editable target %,
  `Price × Frequency` item rows (add/edit/delete), section totals and grand total.
- Wired the three real routes (removing the last placeholders); the nav "Soon" badges are gone.
- Extended EN/ID dictionaries (categories, budget, master-plan, category enums) — kept structurally
  identical. `vue-tsc` + build + 15 Vitest tests green; Docker `web` rebuilt.
- Extended `scripts/seed_demo.py` to seed a current-month budget (plan lines) and master-plan items.
  Verified end-to-end: budget Actual (9,124,000) derives live from the seeded ledger vs Plan
  (6,400,000) → Leftover −2,724,000; master plan grand total 186,600,000 across 40/50/10 sections.

---

## [2026-07-24 16:55:39 UTC]

CHG-0013 — M7 (part 1): Accounts & Transactions screens + demo seed

- Typed API modules `lib/accounts.ts`, `lib/transactions.ts`, `lib/categories.ts` (+ `Paged<T>`,
  Account/Transaction/Category types) and an `errorMessage` helper mapping backend error codes to
  localized text. Shared UI: `AppSelect`, `AppModal` (teleported, Esc/backdrop close).
- **Accounts screen** (`/accounts`): total net worth, account list (type/group/balance, inactive
  dimmed), create/edit modal (name, type, group, opening balance, currency) and deactivate with the
  in-use guard surfaced (`account_in_use`).
- **Transactions screen** (`/transactions`): filtered (type/account/status/search/date range),
  paginated list with signed colored amounts, transfer "A → B" rows, status chips; create modal with
  an Income/Expense/Transfer type toggle and flow-aware budget/category pickers; clear/unclear and
  void row actions. Both wired to the real routes (replacing their placeholders).
- Extended EN/ID dictionaries (accounts, transactions, form/common, error codes) — kept structurally
  identical. `vue-tsc` + build green; 15 Vitest tests green; Docker `web` rebuilt.
- Added `scripts/seed_demo.py` (dev-only): seeds 5 accounts + hundreds of transactions across ~10
  months. Seeded the local stack — net worth Rp 118,429,000 across 5 accounts, 411 transactions.

---

## [2026-07-24 16:41:09 UTC]

CHG-0012 — Dashboard redesign (full, richer layout)

- Reworked the sparse placeholder dashboard into a full, responsive layout: a net-worth hero
  (2/3 width) alongside a compact "This month" income/expense/net summary card; a full-width 12-month
  cashflow chart; and an accounts + recent-activity two-column row with strong empty states and CTAs
  (Add account / Add transaction linking to those sections).
- New `CashflowChart` component: lightweight income-vs-expense monthly bars built with CSS/flex
  (solid fills, semantic green/red, localized month labels, hover tooltips) — no chart dependency,
  matching the design language (no gradients).
- Widened the shell content to `max-w-[1600px]` so it fills wide screens; added the dashboard i18n
  keys (this-month, cashflow, recent, add-account/-transaction, view-all) in both locales.
- Frontend suite green (15 tests); rebuilt the Docker `web` image.

---

## [2026-07-24 16:31:40 UTC]

CHG-0011 — Fix: navigable sidebar + aligned shell layout

- Sidebar/mobile-nav items other than Dashboard had no routes, so they rendered as dead placeholders.
  Every menu item (Transactions, Accounts, Budget, Master Plan, Categories) is now a real route that
  shows a navigable "coming soon" stub (`PlaceholderView`) until M7 builds the screen; active-nav
  highlighting works throughout. Placeholder items show a small "Soon" badge in the sidebar.
- Fixed the shell layout: `main` was centered (`mx-auto max-w-[1200px]`) while the topbar was not,
  producing a large empty left gutter and title/content misalignment. `main` is now left-aligned
  (`max-w-[1400px]`) with the same horizontal padding as the topbar, so the page title and content
  align under the sidebar.
- Added `common.soon` / `common.comingSoonNote` to both locales. Frontend suite green (15 tests);
  rebuilt the Docker `web` image.

---

## [2026-07-24 16:22:00 UTC]

CHG-0010 — Fix: login email field vanished in production (vue-i18n '@' escaping)

- The login email placeholder messages (`you@example.com` / `anda@contoh.com`) contained a literal
  `@`, which vue-i18n parses as its linked-message syntax (`@:key`). In the stricter production i18n
  runtime this broke the email field's render, so only the password field showed. Escaped the `@` as
  `{'@'}` in both locales.
- Added a `LoginView` mount test asserting both the email and password fields render and that the
  placeholder resolves to `you@example.com` (regression guard). Frontend suite: 15 tests green.
- Rebuilt the Docker `web` image with the fix.

---

## [2026-07-24 16:12:09 UTC]

CHG-0009 — M6: Frontend scaffold + shell + login

- New `frontend/` project: Vite + Vue 3 (`<script setup>`) + TypeScript (strict) + Tailwind, Pinia,
  Vue Router, vue-i18n, axios, Lucide. Structure per docs/frontend/FRONTEND_ARCHITECTURE.md.
- Design system: `assets/styles/tokens.css` mirrors the LOCKED design language (dark canvas, single
  solid green `#35D07A`, semantic finance colors, category spectrum, radii) and Tailwind maps
  utilities to those CSS variables — no hardcoded hex, no gradients. Placeholder brand mark + lockup
  SVGs (accumulating bars).
- lib: `format.ts` (id-ID money/number, negatives in parentheses, signed money), `api.ts` (axios at
  `/api/v1`, bearer attach, `{success,data}` unwrap, one-shot 401→refresh rotation, typed
  `ApiClientError`), `session.ts` (token/user storage), and typed API modules `auth.ts`, `reports.ts`.
- State/i18n: Pinia `auth` (login/logout/refresh, persisted session), `ui` (locale + density toggle),
  `theme` (dark v1, ready for a light flip); structurally-identical EN + ID dictionaries; live
  language switching; enum labels (account type, transaction type/status) translated via maps.
- Routing: Vue Router with a tested auth guard (unauthenticated → `/login?redirect=`; authenticated
  away from `/login`).
- UI: responsive `AppShell` — desktop sidebar + top bar (language + sign-out), mobile rounded
  bottom-nav pill; UI kit `AppButton`, `AppCard`, `BalanceCard`, `StatTile`, `StatusChip`,
  `TransactionRow`, `SpendBar`, `Money`, `AvatarChip`, `AppInput`, `FormField`.
- Views: `LoginView` (owner sign-in, localized error mapping) and a live `DashboardView` that renders
  net worth + this month's cashflow from the M5 reports (loading / error+retry / empty states).
- Tooling: `vue-tsc` typecheck + `vite build` green; 14 Vitest tests (formatters + auth guard).
- Docker: added the `web` service (frontend `Dockerfile` multi-stage build → Nginx serving the SPA
  with history fallback and a same-origin `/api` proxy to the API) and `frontend/nginx.conf` +
  `.dockerignore`. `docker-compose.yml` now runs web → api → db; app at `:8091`.
- Verified end-to-end on Docker: SPA served (200), history deep-link fallback, owner login through the
  Nginx→API proxy succeeds, dashboard reads live figures.
- Docs: STATUS, IMPLEMENTATION_PLAN, `frontend/README.md`.

---

## [2026-07-24 15:53:15 UTC]

CHG-0008 — Local Docker stack (API + Postgres) for hands-on testing

- Added `docker-compose.yml`: builds the API image and runs it against a Postgres 16 container. The
  API reaches the DB over the internal compose network, so the database port is not published to the
  host (avoids clashing with a local Postgres on 5433). API is published on `${API_PORT:-8090}` and
  auto-migrates + seeds the owner on startup. The `web` (Nginx + SPA) service is deferred until the
  frontend lands (M6+).
- Fixed `Dockerfile.api`: it copied `Tameru.sln`, but the repo uses `Tameru.slnx` — the image build
  would have failed. Now copies `Tameru.slnx`.
- Added `.dockerignore` (excludes `bin`/`obj`, `.vs`, `tests`, `frontend/node_modules`, `.git`, …) so
  the image builds cleanly and reproducibly from a minimal context.
- Verified: `docker compose up -d --build` → `/health` 200; owner login; the M5 report endpoints
  respond (net worth, cashflow with a 12-point trend); seed taxonomy present.

---

## [2026-07-24 15:45:54 UTC]

CHG-0007 — M5: Reporting (dashboard analytics, compute-on-read)

- Added the Reporting module (Application / Infrastructure / Api). It owns **no data**: no tables, no
  `DbContext`, no migration — every figure is computed on read from other modules' contracts, so
  reports can never drift from the ledger, the single source of truth (ADR-0006). A materialized
  cache is deliberately deferred until data volume would justify it.
  - Application: `ReportingService` with four read use cases returning `Result`s —
    net worth (sum of derived balances over **active** accounts, BR-023, + per-account breakdown),
    monthly cashflow (income vs expense for a month + the 12-month trend), yearly overview (category ×
    month spend matrix), and a category-tracker pivot (daily/monthly over a date range). Query-param
    validation (`ReportingErrors`): month 1..12, known granularity, `from ≤ to`.
  - Infrastructure: `AddReportingModule` (registers the service; no persistence).
  - Api: `/api/v1/reports/{net-worth,cashflow,overview,category-tracker}`, owner-authorized.
- Cross-module contracts: added `IAccountBalanceDirectory` (provided by Accounts — per-account derived
  balances, reusing `AccountService` so the `opening + net movement` formula stays in one place) and
  `ILedgerReportingQuery` (provided by Ledger — monthly income/expense totals and expense totals per
  level-2 category per period bucket). Reporting consumes both; it references no other module directly.
- Bootstrapper: `AddReportingModule` + `MapReportingEndpoints` wired into the host.
- Tests: 10 Reporting unit tests (net-worth active-only, cashflow month + trend, overview/tracker
  pivots ordered by total, granularity/month/date-range validation) and 2 architecture-boundary rules
  (Reporting.Application depends on no Infrastructure/Web and on no other module's internals). Full
  suite green (99 tests).
- Verified end-to-end on Postgres: seeded owner + taxonomy; created an account (opening 5,000,000), a
  3,000,000 income and 500,000 (Jul) + 250,000 (Jun) expenses under Food → net worth 7,250,000; Jul
  cashflow net 2,500,000 with a correct 12-month trend; overview and monthly/daily tracker pivots
  aggregate correctly; invalid month / granularity / inverted range → 400, unauthenticated → 401. An
  Income tagged with an Expense-flow budget envelope is still rejected (`category_flow_mismatch`).
- Docs: MODULES (Reporting implemented; Accounts/Ledger provide the new contracts), ERROR_HANDLING
  (reporting param validation), STATUS, IMPLEMENTATION_PLAN.

---

## [2026-07-24 11:30:00 UTC]

CHG-0006 — M4: Budgeting (Categories, Budget, Master Plan)

- Added the Budgeting module (Domain / Application / Infrastructure / Api):
  - Domain: `Category` (self-referencing Budget→Category→Sub, flow, system flag, BR-040/041/005),
    `BudgetPeriod`/`BudgetLine` (BR-060/061), `MasterPlanSection`/`MasterPlanItem`
    (`TotalBudget = Price × Frequency`, target %, BR-080/081).
  - Application: `CategoryService` (tree CRUD, system + child guards), `BudgetService` (periods +
    lines; Actual/Leftover derived from the ledger via `ICategorySpendQuery`, BR-062),
    `MasterPlanService` (sections/items + roll-ups); `BudgetingErrors`.
  - Infrastructure: `BudgetingDbContext` (schema `budgeting`, snake_case) + configs, repositories,
    `CategoryDirectory` (provides `ICategoryDirectory`), starter-taxonomy + 40/50/10 section seeder,
    DI, design-time factory.
  - Api: `/api/v1/categories`, `/api/v1/budget-periods`, `/api/v1/master-plan`.
- Cross-module contracts: added `ICategoryDirectory` (provided by Budgeting) and `ICategorySpendQuery`
  (provided by Ledger).
- **Ledger extended:** implements `ICategorySpendQuery` (monthly expense totals per category) and now
  validates a transaction's category (exists / active / flow) via `ICategoryDirectory` — closing
  BR-005/006. A permissive no-op directory is used when Budgeting is absent; the bootstrapper
  registers Budgeting last so its real directory wins.
- Bootstrapper: `AddBudgetingModule`, plus Budgeting migrate + seed on startup and endpoint mapping.
- EF migration `Budgeting_Initial`; applied to the dev Postgres; taxonomy + sections seeded.
- Tests: 18 Budgeting unit tests (domain rules; category/budget/master-plan services incl.
  actual-from-ledger) and 2 architecture-boundary rules; updated Ledger tests for the new dependency.
  Full suite green (87 tests).
- Verified end-to-end on Docker: seeded categories + 40/50/10 sections; an Expense with an Income-flow
  category is rejected (`category_flow_mismatch`, 422); a budget's Actual (500,000) derives from a real
  ledger expense against Plan (770,000) → Leftover 270,000.
- Docs: MODULES (Budgeting + Ledger), ERROR_HANDLING (category codes), STATUS, IMPLEMENTATION_PLAN.

---

## [2026-07-24 11:00:00 UTC]

CHG-0005 — M3: Ledger (Income / Expense / Transfer — the cashflow core)

- Added the Ledger module (Domain / Application / Infrastructure / Api):
  - Domain: `Transaction` (single-entry Income/Expense/Transfer) with money rules — amount > 0
    (BR-001), transfer has a distinct target account (BR-002/003), title required; clear/unclear;
    void = soft-delete (BR-007). Pure `BalanceCalculator` (net movement / balance, `asOf` cutoff).
  - Application: `LedgerService` (create/update/clear/unclear/void/list with filters) returning
    `Result`s; validates referenced accounts via the Accounts `IAccountDirectory` contract;
    `LedgerErrors`.
  - Infrastructure: `LedgerDbContext` (schema `ledger`, snake_case), `Transaction` config + indexes,
    repository, and the real `LedgerAccountQuery` implementing `ILedgerAccountQuery` in SQL —
    replacing the Accounts no-op so balances derive live from the ledger (ADR-0006). DI + design-time
    factory.
  - Api: `/api/v1/transactions` (list/filter, get, create, update, clear, unclear, void).
- Bootstrapper: `AddLedgerModule` registered after Accounts (its `ILedgerAccountQuery` overrides the
  no-op); Ledger added to startup migrate and endpoint mapping.
- Fixed the `Result → HTTP` mapper: `account_in_use` (and `category_is_system`, `already_voided`)
  now map to 409 Conflict per ERROR_HANDLING.md.
- EF migration `Ledger_Initial`; applied to the dev Postgres.
- Tests: 25 Ledger unit tests (balance derivation priority + transaction rules + service) and 2
  Ledger architecture-boundary rules. Full suite green (67 tests).
- Verified end-to-end on Docker: income + transfer + expense produce correct account balances
  (A = 8,700,000; B = 7,150,000); void recomputes (B → 7,300,000); deactivating an in-use account is
  blocked (409); type/account/amount validation paths return the right envelopes.
- Docs: MODULES (Ledger status + contracts), ERROR_HANDLING (account_not_found), STATUS,
  IMPLEMENTATION_PLAN updated.

---

## [2026-07-24 10:30:00 UTC]

CHG-0004 — M2: Accounts (accounts, groups, derived balances)

- Added the Accounts module (Domain / Application / Infrastructure / Api):
  - Domain: `Account` (type, opening balance, currency, active, sort order) and `AccountGroup`;
    `BalanceWith(netMovement) = opening + movement` (ADR-0006, BR-022).
  - Application: `AccountService` (list/get/create/update, deactivate with in-use guard BR-021,
    group roll-ups) returning `Result`s; repository + unit-of-work ports; `AccountErrors`.
  - Infrastructure: `AccountsDbContext` (schema `accounts`, snake_case), EF configs, repositories,
    `AccountDirectory` (provides `IAccountDirectory`), `NoOpLedgerAccountQuery` (default
    `ILedgerAccountQuery` until Ledger ships), groups seeder, DI, design-time factory.
  - Api: `/api/v1/accounts` (list/get/create/update/deactivate) and `/api/v1/account-groups`
    (list/create/update), owner-authorized.
- Introduced `src/Modules.Contracts` for interface-only cross-module contracts:
  `IAccountDirectory` (provided by Accounts) and `ILedgerAccountQuery` (provided by Ledger later;
  NoOp for now), so no module references another module's projects directly.
- Bootstrapper: registered the module, mapped endpoints, added Accounts to startup migrate + seed.
- EF migration `Accounts_Initial`; applied to the dev Postgres; default account groups seeded.
- Tests: 13 Accounts unit tests (domain + service incl. balance derivation, deactivate guard,
  group roll-up) and 2 Accounts architecture-boundary rules. Full suite green (40 tests).
- Verified end-to-end on Docker: login → create/list accounts (balance = opening via NoOp ledger)
  → invalid type 400 → deactivate 200 → unauth 401.
- Docs: MODULES, STATUS, IMPLEMENTATION_PLAN updated.

---

## [2026-07-24 10:00:00 UTC]

CHG-0003 — M1: Identity (single-user auth)

- Added the Identity module (Domain / Application / Infrastructure / Api) for the single owner:
  - Domain: `User` (normalized email, locale en/id) and `RefreshToken` (hash-only, rotation-aware).
  - Application: `AuthService` (login, refresh-rotation, logout, get/update profile) returning
    `Result`s; ports `IPasswordHasher`, `ITokenService`, repositories, module unit of work.
  - Infrastructure: `IdentityDbContext` (schema `identity`), EF configs, repositories,
    PBKDF2 `PasswordHasherAdapter`, HMAC-SHA256 `JwtTokenService`, owner seeder, DI, design-time
    factory; snake_case columns via `EFCore.NamingConventions` (ADR-0007).
  - Api: `/api/v1/auth/login|refresh|logout|me` (minimal APIs) using the response envelope; a
    reusable `Result → HTTP` mapper in Web.Common.
- Bootstrapper: JWT bearer authentication, `HttpCurrentUser` (claims → audit), module registration,
  Swagger bearer button, and startup auto-migrate + seed.
- Initial EF migration `Identity_Initial`; applied to the dev Postgres. Verified end-to-end:
  login → `/me` → refresh rotation, plus 401/invalid-credentials paths.
- Tests: 21 Identity unit tests (domain + AuthService incl. rotation/reuse) and 2 new Identity
  architecture-boundary rules. Full suite green (25 tests).
- Dev infra: `docker-compose.dev.yml` now maps Postgres to host port 5433 (5432 was reserved);
  `appsettings.Development.json` wired for the dev DB, JWT, and owner seed.
- Docs: ADR-0007; DATABASE (naming convention), DEPLOYMENT (local run + dev creds), STATUS updated.

---

## [2026-07-24 09:30:00 UTC]

CHG-0002 — M0: solution & BuildingBlocks scaffold

- Created the .NET 8 solution (`Tameru.sln`), `Directory.Build.props` (net8.0, nullable on,
  warnings-as-errors) and `.editorconfig`.
- BuildingBlocks:
  - `Tameru.SharedKernel` — `Entity`/`AuditableEntity`, `ValueObject`, `Money` (IDR, numeric(19,2)),
    `Result`/`Error`, `PagedResult`, `IClock`/`SystemClock`, `DomainRuleException`, domain-event and
    audit/soft-delete markers.
  - `Tameru.Application.Abstractions` — `ICurrentUser`, `IUnitOfWork`, in-process messaging markers.
  - `Tameru.Infrastructure.Common` — `BaseDbContext` (audit stamping + physical-delete→soft-delete +
    soft-delete global query filter) with EF Core + Npgsql.
  - `Tameru.Web.Common` — `ApiResponse` envelope and `ExceptionHandlingMiddleware`
    (DomainRuleException→422, unhandled→500 with traceId; no leaks).
- `Bootstrapper/Tameru.Api` — minimal host with Swagger, CORS, DI for `IClock`/`ICurrentUser`,
  exception middleware, and a `/health` endpoint returning the standard envelope (verified 200).
- `tests/Tameru.ArchitectureTests` — NetArchTest fitness rules (SharedKernel and
  Application.Abstractions have no outward/infrastructure dependencies). `dotnet build` and
  `dotnet test` green (2 passed, 0 warnings).
- Infra: `docker-compose.dev.yml` (Postgres, loopback-only), `Dockerfile.api` (multi-stage),
  `.env.example`.
- Added [docs/IMPLEMENTATION_PLAN.md](docs/IMPLEMENTATION_PLAN.md); updated docs index and STATUS.

---

## [2026-07-24 00:00:00 UTC]

CHG-0001 — Project documentation & design baseline

- Established Tameru as a single-user, single-entry personal finance manager derived from the
  `Financial Projection (Indonesia)` workbook, on AccounTrack's architecture and engineering
  discipline.
- Authored the initial documentation set: `CLAUDE.md`, `README.md`, and `docs/` (product,
  architecture, data model, API, rules, decisions, security, engineering, and frontend design).
- Locked the four founding decisions (single-user, single-entry cashflow, MVP scope, dark-first
  green-accent UI) as ADR-0001…ADR-0004 in [docs/DECISIONS.md](docs/DECISIONS.md).
- No application code yet — see [docs/STATUS.md](docs/STATUS.md) for what's next.
