# UX Audit — Tameru web app (all MVP screens)

| | |
|---|---|
| Scope | The seven authenticated screens (Dashboard, Transactions, Accounts, Reports, Budget, Master Plan, Categories), the Login screen, and the shared shell (sidebar, topbar, mobile nav, modals, confirm dialog). Responsive web, dark theme only (ADR-0004). |
| Standards | Nielsen's 10 heuristics, WCAG 2.2 AA, `docs/frontend/DESIGN_LANGUAGE.md` design tokens, and the `CLAUDE.md` non-negotiable rules (bilingual EN/ID, no hardcoded UI text, dark-first single green accent, id-ID money). |
| Inputs | Live Docker stack (`web` :8091, `api` :8090) with the seeded owner and real data (5 accounts, 822 transactions); frontend source at `frontend/src`; 28 rendered captures at 1440 / 768 / 375 / 320 px plus rendered-DOM dumps. |
| Date | 2026-09-12 |
| Depth | Full audit |

## 1. Summary

Tameru is in good shape. The design system is genuinely well disciplined — every colour comes from a
token, all body/label text passes contrast comfortably (16.9:1 for primary text, 5.2:1 for muted),
the responsive work holds at every width down to 320 px with no page-level horizontal scroll, and the
Indonesian localisation is deeper than most products manage (months, category names, and even number
abbreviations `jt`/`rb` are translated). The M9 hardening pass clearly did its job.

The problems that remain are concentrated in three places: **one control that silently does nothing**,
**two modules that cannot be reached on a phone**, and **keyboard/assistive-technology gaps in the
dialogs** that carry the money-critical flows. Three findings are Critical, and all three are small,
well-contained code changes rather than redesigns.

The highest-value fixes, in order: wire up the transactions search (AUD-001, one line), give the
mobile nav a route to Master Plan and Categories (AUD-002), and add focus management plus an
accessible name to `AppModal`/`ConfirmDialog` (AUD-003, AUD-010). After those, the wordmark bug
(AUD-004) is a two-character fix that restores half the product's logo.

| Severity | Count |
|---|---|
| 4 — Critical | 3 |
| 3 — Major | 8 |
| 2 — Minor | 9 |
| 1 — Cosmetic | 3 |

## 2. What Works Well

Worth protecting in any future change:

- **Token discipline is real.** `tailwind.config.ts` maps every utility to a CSS custom property and
  contains no hard-coded hex. A grep for stray colour literals in components found none.
- **Contrast of text is comfortably above AA**, not scraped past it: `--text` 16.9:1, `--text-muted`
  5.72:1 on `--bg` and 4.59:1 even on `--surface-2`, `--accent` 9.18:1, `--negative` 6.08:1.
- **The Reports heat-map is a rare accessible heat-map.** Fill intensities are capped so the worst
  cell still measures 5.08:1 for its value text, and every cell shows the number as well as the
  colour, so nothing is encoded by colour alone.
- **Indonesian is a first-class locale, not a veneer.** Nav, headings, empty states, month
  abbreviations (`MEI`, `AGU`, `DES`), seeded category names (`Makanan`, `Hiburan`), and magnitude
  suffixes (`jt`, `rb`) all switch. `<html lang>` correctly follows the stored locale on boot. No
  layout broke under the longer Indonesian strings.
- **Responsive behaviour holds.** No document-level horizontal overflow at 1440, 768, 375 or 320 px
  on any screen; wide data tables are correctly placed in their own `overflow-x:auto` containers
  rather than forcing the page to scroll.
- **Destructive actions are explained, not just confirmed.** "Void this transaction? It is
  soft-deleted and stops affecting balances." tells the user the consequence, which is exactly right
  for a ledger that never hard-deletes.
- **Empty states are designed.** "No budget for this month yet." with a `Create budget` primary
  action is a proper empty state, not a blank card.
- **Login is solid**: both fields have real `<label for>` associations, correct `autocomplete`
  (`username` / `current-password`), a visible 2 px green focus ring, and errors are exposed through
  `role="alert"`.
- **Escape closes both the modal and the confirm dialog**, and `prefers-reduced-motion` is honoured
  globally in `main.css`.

## 3. Findings

| ID | Severity | Criterion | Location | Finding | Evidence | Recommendation | Effort |
|---|---|---|---|---|---|---|---|
| AUD-001 | 4 — Critical | H1, H5 | `frontend/src/views/transactions/TransactionsView.vue:321` | The Transactions **search box does nothing**. It writes `filters.q` but never calls `applyFilters()`; there is no watcher, debounce, Enter handler, or submit button, and the input is not inside a `<form>`. The value is only ever sent if the user afterwards touches one of the other five filters. On a 822-row ledger this is the primary way to find anything. | Driven in the live app: typing `Salary` produced **0 API calls**; pressing Enter produced **0 API calls**; then changing the Type filter fired `/api/v1/transactions?page=1&pageSize=25&type=Income&q=Salary` — proving the backend supports `q` and the value was sitting unused. | Call `applyFilters()` from the search input, debounced (~300 ms), exactly as the five sibling controls already do. Also wrap the filter row in a `<form @submit.prevent>` so Enter works. | S |
| AUD-002 | 4 — Critical | H3, H4 | `frontend/src/components/layout/navItems.ts:27`, `AppSidebar.vue:13` | **Master Plan and Categories are unreachable on a phone.** The sidebar is `hidden … md:flex` so it disappears below 768 px, and the mobile bottom nav is hard-coded to `navItems.slice(0, 5)`. There is no overflow, "More", or hamburger affordance, so two of the seven modules have no navigation path at all under 768 px — only a manually typed URL. | `mobileNavItems = navItems.slice(0, 5)` yields Dashboard, Transactions, Accounts, Reports, Budget. Verified at 375×667: the rendered `nav.fixed` contains exactly those five links. | Add a 6th "More" item opening a sheet with the remaining destinations, or make the pill horizontally scrollable. Keep five *visible* for thumb reach — the constraint in the code comment is sound, the missing overflow is the bug. | M |
| AUD-003 | 4 — Critical | WCAG 2.4.3, 4.1.2 | `frontend/src/components/ui/AppModal.vue` | **The modal has no focus management and no accessible name.** Focus is not moved into the dialog on open, focus is not trapped, and it is not restored on close. Keyboard and screen-reader users tab straight through the page behind the open dialog. `role="dialog"` and `aria-modal="true"` are set, but there is no `aria-label`/`aria-labelledby`, so the dialog is announced without a name. This is the Add/Edit transaction flow — the app's core money-entry path. | With the Add transaction modal open, all **18 of the first 18 tab stops resolved outside the dialog** (sidebar links, topbar buttons, page buttons, filter selects). `document.activeElement` was `BODY` immediately after open. | Move focus to the first control (or the dialog) on open; trap Tab/Shift+Tab within the dialog; restore focus to the trigger on close; add `aria-labelledby` pointing at the existing `<h2>`. Apply the same to `ConfirmDialog.vue` and `ImportModal.vue`. | M |
| AUD-004 | 3 — Major | WCAG 1.4.3, brand | `frontend/src/assets/brand/logo-lockup.svg:7` | **Half the wordmark is invisible.** The "Ta" tspan uses `fill="currentColor"`, but the lockup is loaded via `<img src>` (`AppSidebar.vue:18`), and an SVG referenced as an image is an isolated document — `currentColor` resolves to the initial black, not the page's text colour. On the `#0f1011` sidebar that is ≈1.06:1, so the logo reads **"meru"** on every screen. Self-hosting Inter (M9 P8) could not have fixed this: an `<img>`-loaded SVG cannot see the page's `@font-face` either, so the `<text>` element also falls back to a different font. | 4× crop `design/screenshots/wordmark-ta-invisible.png`; sampled glyph pixels are `(0,0,0)` against the `#0f1011` sidebar. | Replace `currentColor` with the explicit token value `#F4F5F6`, and convert the `<text>` element to outlined paths (or inline the SVG in the component) so the wordmark no longer depends on a font the isolated document cannot load. | S |
| AUD-005 | 3 — Major | WCAG 1.4.3 | `frontend/src/components/ui/AppButton.vue:24` | The **danger button fails contrast**: `bg-negative text-white` is white on `#ff5b60`. It is the confirm button for voiding a transaction — a money-critical destructive action. | Measured from the rendered confirm dialog: fg `(255,255,255)` on bg `(255,91,96)` = **3.04:1**, below the 4.5:1 minimum. | Mirror the primary button's pattern (`bg-accent text-accent-contrast`): introduce `--negative-contrast: #0b0f0c` and use dark text on the red — measured **6.36:1**. Alternatively darken the fill to `#c92a2f` and keep white text (5.45:1). | S |
| AUD-006 | 3 — Major | WCAG 1.3.1, 4.1.2 | `frontend/src/views/transactions/TransactionsView.vue:318-323` | The six **filter controls have no labels** — no `<label>`, no `aria-label`. The two date inputs are especially bad: native date fields cannot show a placeholder, so nothing on screen or in the accessibility tree distinguishes the "from" field from the "to" field. | `audit_html.py` on the rendered DOM: 6 × `control-label [WCAG 1.3.1, 4.1.2] form control has no label` (one noting "placeholder is not a label"). | Add `aria-label` to each control (`transactions.filter.type`, `…account`, `…status`, `common.search`, `common.dateFrom`, `common.dateTo`). `AppInput`/`AppSelect` are single-root, so the attribute falls through with no component change. Add the six keys to both locale files. | S |
| AUD-007 | 3 — Major | WCAG 4.1.2 | `frontend/src/views/budget/BudgetView.vue:123,125` | The Budget **month prev/next buttons have no accessible name** — raw `<button>` elements containing only a Lucide chevron. They bypass the `IconButton` component that M9 introduced precisely to guarantee a label and tooltip. | `audit_html.py` on the rendered Budget DOM: 2 × `button-name [WCAG 4.1.2] button has no accessible name`. | Replace both with `<IconButton :icon="ChevronLeft" :label="t('common.previousMonth')" />`. Reports' year nav already does this correctly — follow it. | S |
| AUD-008 | 3 — Major | WCAG 1.4.11 | `frontend/src/assets/styles/tokens.css` (`--border`), `AppInput.vue:22`, `AppSelect.vue:22` | **Form field boundaries fail non-text contrast.** `--border` `#2b2e31` on `--surface` `#1c1d1f` is **1.24:1** (and 1.35:1 on `--bg`), against a 3:1 requirement. Since that border is the only thing defining the edge of every text input and select, the fields are hard to locate. | `design_tokens.py contrast`: `#2b2e31` on `#1c1d1f` = 1.24:1 FAIL; on `#131415` = 1.35:1 FAIL. | Introduce a distinct `--border-strong` (≥3:1 against `--surface`, e.g. `#5a6068` at 3.03:1) for **interactive control** borders, and keep the current subtle `--border` for decorative card dividers, where 1.4.11 does not apply. | S |
| AUD-009 | 3 — Major | WCAG 1.4.10, platform | `frontend/src/components/layout/MobileNav.vue` | The **mobile bottom nav overflows the screen at 375 px** — the explicitly stated iPhone 8 target from the M9 P2 responsive pass. The five English labels do not fit, so the last item ("Budget") is clipped by the viewport edge and the pill's rounded right edge is cut off. | Measured at 375×667: the last link's `right` = **382.7 px** against a 375 px viewport. Link widths sum to 373.8 px, plus 32 px outer margin and 16 px padding = 421.8 px required in 375 px available. See `design/screenshots/mobile-375-nav-clipped.png`. | Let labels shrink (`text-[10px]`, `min-w-0`, `truncate`) and reduce `px-3` to `px-2` on each link; or drop labels to icon-only below 380 px with `aria-label` retained. Re-check with Indonesian, where `Transaksi`/`Anggaran` differ in length. | S |
| AUD-010 | 3 — Major | H5, H9, WCAG 2.4.3 | `frontend/src/components/ui/ConfirmDialog.vue` | The void confirmation **does not say which transaction it will void**. The copy is generic ("this transaction"), the dialog has no title in this call site, and it covers the list, so the target row is obscured at the moment of decision. Focus is also not moved into the dialog (same root cause as AUD-003). | `design/screenshots/void-confirm-danger-button.png`; after opening, `document.activeElement` was `BODY`. | Pass the transaction's title, date and amount into the confirm message (e.g. "Void **Salary**, Rp 12.200.000 on 25 Jul 2026?"), and give the dialog a title. For a ledger action this identification matters more than the wording of the warning. | S |
| AUD-011 | 3 — Major | WCAG 4.1.2 | `ReportsView.vue:145`, `TransactionsView.vue:393` (type selector) | **Segmented controls do not expose their selected state.** The Yearly/Monthly/Daily toggle and the Income/Expense/Transfer type selector are plain `<button>`s whose selection is conveyed only by background and text colour. Assistive technology is given no state at all. | Source inspection: active state is `:class="… ? 'bg-accent-soft text-accent' : …"` with no `aria-pressed`, `aria-current`, or `role="radiogroup"`. | Add `:aria-pressed="granularity === g"` (toggle group), or model as `role="radiogroup"` + `role="radio" :aria-checked`. Visual treatment already changes background as well as colour, so 1.4.1 is satisfied — only the programmatic state is missing. | S |
| AUD-012 | 2 — Minor | H6, WCAG 1.1.1 | `components/ui/SpendBar.vue`, `DashboardView.vue:104`, `ReportsView.vue:177-183` | The **segmented spend bar has no key**. On Reports the legend lists category names and amounts but carries **no colour swatches**, so segments cannot be mapped to categories. On the Dashboard the same bar sits under net worth with **no legend at all** — three coloured segments with nothing explaining them. The only affordance is a `title` tooltip, which is unavailable to keyboard and touch users. The bar also has no `role="img"`/`aria-label`, so it is invisible to screen readers. Separately, reusing the green→amber→red category spectrum for *accounts* reads as good→bad in a finance UI, implying something is wrong with the 5th account. | `design/screenshots/dashboard-1440.png` (bar, no legend) and `reports-1440.png` (legend, no swatches). | Add a colour chip to each legend row using the same `spectrum[i]`; add a legend to the Dashboard bar or drop the bar there; give `SpendBar` a `role="img"` with an `aria-label` summarising the split; and use a neutral sequence for account composition rather than the semantic red. | S |
| AUD-013 | 2 — Minor | H2, i18n | `AppInput` date usages (`TransactionsView.vue:322-323,408`) | **Dates render US-style.** Native date inputs show `mm/dd/yyyy`, and the Add-transaction modal shows `09/12/2026`, in a product whose stated display convention is id-ID. For an Indonesian user `09/12/2026` is genuinely ambiguous (9 Dec vs 12 Sep). | `design/screenshots/transactions-1440.png` (filter bar) and `add-transaction-modal.png`. | The native control's format follows the browser locale and cannot be overridden by CSS. Either add a visible `dd/mm/yyyy` hint next to the field, or use a small custom date control that formats per the app locale. At minimum label the two filter dates (AUD-006). | M |
| AUD-014 | 2 — Minor | H1 | `frontend/src/views/DashboardView.vue:108` | The dashboard's **"This month" card never names the month**, and when the current month has no data it shows `Rp 0 / Rp 0 / Rp 0` with no explanation, next to "Expenses by category → No expenses this month." A user returning after a gap sees an apparently empty dashboard and cannot tell whether it is a data problem or simply a new month. | Current date 2026-09-12, latest data July 2026 → `design/screenshots/dashboard-1440.png` shows all-zero tiles while the 12-month chart beside it is full. | Render the month name ("This month · September 2026"), and when the figure is zero add a quiet hint linking to the last month with activity. | S |
| AUD-015 | 2 — Minor | WCAG 2.5.8 | `DashboardView.vue` ("View all"), `MasterPlanView.vue:156` | **Targets below the 24×24 px minimum.** The Dashboard "View all" links measure 66×20 px, and the Master Plan target chips ("Target 40%") 78×21 px. Neither qualifies for the inline-text exception, as both are standalone controls. | Measured in the live DOM across all seven routes; these were the only sub-24 px targets found — everything else passes, and the mobile nav links are a healthy 50.5 px tall. | Add `py-1` (or `min-h-[24px]`) to both. Everywhere else already complies, so this is a spot fix, not a systemic issue. | S |
| AUD-016 | 2 — Minor | WCAG 2.4.6, H8 | `AppTopbar.vue:33` + each view's page heading | **Every page except Dashboard shows its title twice** and ships two `<h1>` elements — once in the topbar, once as the page heading directly beneath it. The duplication wastes the most valuable strip of the screen, and Dashboard's inconsistency (only one) shows the second is not needed. | `audit_html.py`: `heading-h1-multiple` on 6 of 7 screens (Dashboard clean). Visible in every 1440 screenshot. | Keep the topbar `<h1>` as the page title and drop the in-page duplicate (or vice-versa), leaving one `<h1>` per page. Reclaim the row for the page's actions. | S |
| AUD-017 | 2 — Minor | WCAG 4.1.2 | `frontend/src/components/ui/ImportModal.vue:72` | The **Import modal's close button has no accessible name**, unlike `AppModal`'s, which does. Same visual control, two different implementations. | Source comparison: `AppModal.vue:31` sets `:aria-label`, `ImportModal.vue:72` sets none. | Use the same labelled close control in both — ideally extract it, so the two modals cannot drift again. | S |
| AUD-018 | 2 — Minor | `CLAUDE.md` rule 9 | `frontend/src/components/ui/AppModal.vue:31` | **Hardcoded English UI string**: `:aria-label="'Close'"`. The project's non-negotiable rule 9 requires every user-facing string — explicitly including accessible names — to go through i18n and exist in both locales. An Indonesian screen-reader user hears "Close". | Source. | Replace with `$t('common.close')` and add the key to `en.ts` and `id.ts` (`Close` / `Tutup`). | S |
| AUD-019 | 2 — Minor | H6 | `frontend/src/views/transactions/TransactionsView.vue` (row date) | **Transaction rows show no year** ("Jul 26"). With 822 rows spanning multiple years and 33 pages, a row's year is unknowable without opening it, and "Jul 26" can also be misread as a date-month pair. | `design/screenshots/transactions-1440.png`. | Show the year when a row's year differs from the current one, or insert a sticky month/year divider between date groups. | S |
| AUD-020 | 2 — Minor | H3, H5 | `frontend/src/components/ui/AppModal.vue:19` | **Clicking the backdrop discards a part-filled form** without warning (`@click.self="emit('close')"`), as does Escape. On the Add-transaction form that silently throws away typed input. | Source; verified Escape closes the modal immediately. | Keep click-outside and Escape, but confirm before discarding when the form is dirty, or reinstate the entered values when the modal is reopened. | S |
| AUD-021 | 1 — Cosmetic | H8 | `frontend/src/views/transactions/TransactionsView.vue` (status chip) | The green **"Cleared" chip repeats on essentially every row**, adding 25 chips per page of visual noise while carrying almost no information; only the exception (amber "Uncleared") matters. | `design/screenshots/transactions-1440.png` — 24 of 25 rows show it. | Render the chip only for non-cleared statuses, or reduce cleared to a small neutral dot. | S |
| AUD-022 | 1 — Cosmetic | H5 | `frontend/src/views/transactions/TransactionsView.vue:411` | The **Amount field is pre-filled with `0`**, which the user must select and delete before typing. The field also carries no `Rp` affordance, in an app where every amount has an explicit currency. | `design/screenshots/add-transaction-modal.png`. | Start empty with a `0` placeholder, and prefix the field with the account's currency code. | S |
| AUD-023 | 1 — Cosmetic | — | `frontend/src/views/budget/BudgetView.vue` (period load) | A month with no budget is fetched and **returns HTTP 404**, logging a console error on every visit. The UI handles it correctly — this is a normal, expected state, not an error — but using a 404 for it produces permanent console noise and would mask a real fault. | Network capture: `404 /api/v1/budget-periods/2026/9` on every Budget page load; the screen nonetheless renders the correct "No budget for this month yet." empty state. | Return `200` with an empty/null period (or catch the 404 explicitly without logging) so the console stays meaningful. | S |

## 3a. Resolution status (2026-09-12)

All findings were implemented in the same pass except the two noted below. Re-verified against the
rebuilt Docker stack: `audit_html.py` now reports **0 errors and 0 warnings on all seven screens**
(was 8 errors, 6 warnings), and no screen has page-level horizontal overflow at 1440/375/320px.

| ID | Status | Verified by |
|---|---|---|
| AUD-001 | Fixed | Typing `Salary` now fires `…/transactions?page=1&pageSize=25&q=Salary` (1 API call); was 0. Debounced at 300ms, plus form submit on Enter. |
| AUD-002 | Fixed | Mobile pill is now `Dashboard · Transactions · Accounts · Reports · More`; the More sheet reaches Budget, Master Plan and Categories. |
| AUD-003 | Fixed | All 14+ tab stops now resolve **inside** the dialog (was 18/18 outside); focus moves to the dialog on open and is restored on close; `aria-labelledby` set. |
| AUD-004 | Fixed | Wordmark renders "Tameru" in full; wordmark is now real HTML text via `components/brand/LogoLockup.vue`. |
| AUD-005 | Fixed | Danger button computes `rgb(11,15,12)` on `rgb(255,91,96)` = **6.36:1** (was white, 3.04:1). |
| AUD-006 | Fixed | `audit_html.py` control-label errors: 6 → 0. |
| AUD-007 | Fixed | `audit_html.py` button-name errors: 2 → 0. |
| AUD-008 | Fixed | New `--border-strong` `#6B727B` = **3.47:1** on `--surface` (the first candidate, `#5A6068`, measured 2.66:1 and was rejected). |
| AUD-009 | Fixed | Last nav link now ends at 289.8px inside a 375px viewport (was 382.7px, clipped). |
| AUD-010 | Fixed | Confirm is now `role="alertdialog"`, focus moves into it, and it is labelled/described. |
| AUD-011 | Fixed | `aria-pressed` on the Reports granularity toggle and the transaction type selector. |
| AUD-012 | Fixed | Legend swatches on Reports; a full legend added to the Dashboard net-worth bar; `SpendBar` gained `role="img"` with a percentage summary. Segments now exclude non-positive balances so every legend entry maps to a drawn segment. |
| AUD-013 | Partial | Both date filters are now labelled (`aria-label` + `title`). The **`mm/dd/yyyy` display remains**: a native date input follows the browser locale and cannot be overridden. A custom date control is the only full fix. |
| AUD-014 | Fixed | "This month" now shows "September 2026". |
| AUD-015 | Fixed | No sub-24px targets remain. (The measurement flags a 1×1 `sr-only` submit button, which is visually hidden and not a pointer target.) |
| AUD-016 | Fixed | One `<h1>` per page; `heading-h1-multiple` warnings: 6 → 0. |
| AUD-017 | Fixed | Import modal close button labelled, and the modal now uses the shared focus trap. |
| AUD-018 | Fixed | `$t('common.close')`; `common.close` already existed in both locales. |
| AUD-019 | Fixed | `formatRowDate` appends the year only for rows outside the current year; covered by 4 new unit tests. |
| AUD-020 | Fixed | `AppModal` takes a `dirty` prop and asks before discarding, using the project's own confirm dialog rather than a native one. |
| AUD-021 | Fixed | The status chip renders only for non-cleared rows. |
| AUD-022 | Fixed | Amount starts empty with a `0` placeholder and is prefixed with the account's currency code. |
| AUD-023 | **Not done** | Deliberately deferred: the frontend already handles the 404 correctly as an empty state, and removing the console noise means changing the API contract (`GET /budget-periods/{y}/{m}` returning 200 with a null period), which touches `API_SPEC.md` and the backend tests. Worth doing, but it is a backend change, not a UI one. |

**One item needs a product decision, not a fix.** The net-worth bar still uses the green→amber→red
category spectrum for *accounts*, which reads as good→bad in a finance UI. Adding a second, neutral
palette would be a design-system change, so per the design-system consistency gate it is left for
sign-off rather than introduced unilaterally. The legend added by AUD-012 removes the
"cannot map segments" half of the problem.

Checks run after the fixes: `vue-tsc` typecheck, `vitest` (23 tests, 4 new), a production build, a
Docker rebuild, `audit_html.py` on all seven rendered screens, token contrast re-measurement,
keyboard tab-order walks, and fresh captures at 1440/375/320px.

## 4. Prioritized Plan

1. **Quick wins — high severity, small effort.** `AUD-001` (search does nothing), `AUD-004`
   (wordmark), `AUD-005` (danger button contrast), `AUD-006` (filter labels), `AUD-007` (Budget month
   buttons), `AUD-008` (input border token), `AUD-009` (mobile nav clipping), `AUD-011` (segmented
   control state). All are single-file, low-risk changes; together they clear both remaining
   `audit_html.py` errors and both measured contrast failures.
2. **Next — high severity, more design work.** `AUD-002` (mobile route to Master Plan and
   Categories), `AUD-003` + `AUD-010` (focus management, dialog naming, and identifying the
   transaction being voided). `AUD-003` is best solved once in `AppModal` and reused by
   `ConfirmDialog` and `ImportModal`.
3. **Later — polish.** `AUD-012` … `AUD-023`, of which `AUD-012` (spend-bar legend) and `AUD-016`
   (duplicated page title) give the most visible improvement for the effort.

A sensible split is three PRs matching the project's existing rhythm — accessibility fixes, mobile
navigation, then visual/content polish — each tested on Docker before merge.

## 5. Accessibility Summary

Status against WCAG 2.2 AA, from automated checks on the rendered DOM plus manual keyboard and
measurement passes.

| Principle | Status |
|---|---|
| Perceivable | Text contrast passes everywhere measured, including the heat-map (worst 5.08:1). Two failures: the danger button at 3.04:1 (AUD-005) and the invisible "Ta" in the wordmark at ~1.06:1 (AUD-004). One non-text-contrast failure: control borders at 1.24:1 (AUD-008). The spend bar conveys information with no text alternative (AUD-012). |
| Operable | Keyboard reachable with a clearly visible 2 px green focus ring (`:focus-visible`), and no keyboard traps. The significant gap is dialogs: no focus move, trap, or restore (AUD-003, AUD-010). Two target-size misses (AUD-015). No page-level horizontal scroll at 320 px; wide tables scroll within their own containers, which is the correct pattern. Reduced motion is honoured. |
| Understandable | `<html lang>` correctly tracks the locale on boot and on switch. Login errors use `role="alert"`. Six unlabelled filter controls (AUD-006) and US-formatted dates in an id-ID product (AUD-013) are the weak points. |
| Robust | `role="dialog"` + `aria-modal="true"` are set correctly; missing accessible names on the dialogs (AUD-003), the Budget month buttons (AUD-007), the Import close button (AUD-017), and missing state on segmented controls (AUD-011). |

**Automated results.** `audit_html.py` over the rendered DOM of all seven screens: **8 errors, 6
warnings** — 6 × `control-label` (Transactions), 2 × `button-name` (Budget), 6 × `heading-h1-multiple`.
Dashboard was completely clean. `design_tokens.py contrast` over 15 token pairs: 13 pass, 2 fail
(both `--border`).

**Manual checks performed.** Tab-order walk with the modal open (18 stops), Escape behaviour on modal
and confirm dialog, focus-ring visibility, target-size measurement across all seven routes, live
filter behaviour with network tracing, full Indonesian pass, and rendering at 1440 / 768 / 375 / 320 px
plus a true 1440×900 browser window.

## 6. Not Verified

- **Screen readers.** No NVDA, JAWS, or VoiceOver run. Findings about announcement are inferred from
  the accessibility-relevant markup, not heard. AUD-003 in particular deserves a real screen-reader
  pass once fixed.
- **Real devices.** All mobile observations come from Chrome device emulation at 375×667 and 320 px.
  Physical iPhone 8 / Android behaviour — notably safe-area insets and the on-screen keyboard over
  the fixed bottom nav — is unconfirmed.
- **400 % zoom reflow (WCAG 1.4.10).** Checked by narrow-viewport emulation only, not by true browser
  zoom.
- **Light theme.** Not audited: v1 is dark-only by ADR-0004, and the theme store hard-codes `dark`.
- **Performance and Core Web Vitals.** Not measured; no Lighthouse run. Perceived-performance
  observations are limited to the presence of skeletons.
- **Import flows end to end.** The Import modal was inspected in source but not driven through a real
  CSV upload, so its preview, validation, and error-reporting states are unaudited. The seeded data
  contains obvious double-imports and `IMPORT TEST` rows, which suggests duplicate detection is worth
  reviewing when that flow is audited.
- **A "squeezed" cashflow chart** seen in early captures was traced to CDP viewport emulation, **not**
  a product defect: on a genuine 1440×900 browser window the chart lays out correctly and resizes
  with the container. It is recorded here only so it is not re-reported.

## Appendix

- **Screenshots:** `design/screenshots/` — seven screens at 1440 px, the 375 px mobile viewport, the
  Add-transaction modal, the void confirm dialog, the 4× wordmark crop, and the Indonesian Reports
  screen.
- **Render results:** `design/screenshots/_render-results.json` — per-screen console errors and
  horizontal-overflow measurements for all 28 captures.
- **Tooling:** `audit_html.py` (rendered DOM), `design_tokens.py contrast` (token pairs), and a
  Chrome DevTools Protocol driver used to log in, capture every route at four widths, drive the
  keyboard, and trace network requests.
