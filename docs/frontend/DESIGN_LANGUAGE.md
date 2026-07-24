# Design Language

The single source of truth for Tameru's visual styling. These tokens map to the Tailwind theme +
CSS custom properties at build time so the app stays consistent.

> **State: 🟢 LOCKED (v1).** Derived from the product owner's reference (a dark fintech mobile app
> with a **solid green** accent). Non-accent hexes are eyeballed from the reference (_approx_) and may
> be nudged for contrast during build, but the palette structure and the accent are fixed.

## Locked decisions (ADR-0004)

- ✅ **Dark-first.** One dark theme for v1; a light theme is deferred.
- ✅ **Accent = solid green `#35D07A`.** Used **sparingly**: primary buttons (dark text on green),
  active nav, focus ring, key chart series, positive deltas. **No gradients anywhere.**
- ✅ **Separate finance semantics** (positive green, negative red, warning amber) so the accent
  never doubles as a status color.
- ✅ **Rounded, soft cards** on a near-black charcoal canvas; generous radius; avatar chips for
  people/merchants; a **segmented multi-color spend bar** for category breakdown (from the reference).
- ✅ **Typography = Inter** (SIL OFL 1.1). Optionally **Space Grotesk** for large display numbers.
- ✅ **Icons = Lucide** (MIT). **No emoji.**
- ✅ **Locale/number default = `id-ID`** (`1.234.567`); functional currency **IDR**; **tabular
  figures** on all numbers; negatives in parentheses.
- ✅ **No gradients, no glassmorphism, no stock illustrations, no purple.**

## 0. Principles

- **Calm, dark, confident** — a personal money app used daily; clarity over decoration.
- **Data-first** — money/tables/forms are first-class; tabular figures everywhere numbers appear.
- **Crafted, not templated** — one solid accent, real line icons, consistent 4px rhythm, flat fills.
- **Accessible** — WCAG AA contrast, full keyboard support, visible green focus ring.

## 1. Color

### Accent (solid green)
| Token | Value | Notes |
|---|---|---|
| `--accent` | `#35D07A` | primary button bg, active nav, links, key chart series |
| `--accent-hover` | `#2FBB6D` | hover |
| `--accent-active` | `#29A561` | pressed |
| `--accent-soft` | `rgba(53,208,122,.14)` | selected rows, soft badges, active-nav tint |
| `--accent-contrast` | `#0B0F0C` | **dark** text/icon on green fills |

### Neutrals (dark)
| Role | Value _approx_ | Notes |
|---|---|---|
| `--bg` (canvas) | `#131415` | app background (near-black charcoal) |
| `--surface` (card) | `#1C1D1F` | cards, menus, inputs |
| `--surface-2` | `#26282B` | elevated fills, table header, hover, nav pill |
| `--sidebar` | `#0F1011` | desktop sidebar / mobile bottom-nav container |
| `--border` | `#2B2E31` | hairline dividers/borders |
| `--text` | `#F4F5F6` | primary |
| `--text-muted` | `#8A9097` | labels, secondary, timestamps |

### Semantic (distinct from accent)
| Role | Value | Used for |
|---|---|---|
| `--positive` | `#35D07A` | income, +delta, cleared, gains (shares the green) |
| `--negative` | `#FF5B60` | expense/overspend, −delta, validation errors |
| `--warning` | `#FFB020` | pending / uncleared / due soon |
| `--info` | `#4C9AFF` | informational |

### Category spectrum (segmented spend bar & donut)
A fixed ordered palette for category breakdown, from the reference's multi-color bar:
`#35D07A` · `#9BE15D` · `#FFC531` · `#FF8A34` · `#FF5B60` · `#4C9AFF` · `#B06BFF` (extend by rotation).

> **Finance note:** negative amounts render `(1.234.567)` in `--negative`; income renders in
> `--positive`. Status chips use the semantic palette, never the accent — so a green primary button
> never reads as a "cleared" status.

## 2. Typography

- **Family:** **Inter** (self-hosted woff2). Optional **Space Grotesk** for large KPI/display numbers.
- **Numerics:** `font-variant-numeric: tabular-nums lining-nums` on every amount/qty; **id-ID**
  formatting by default.
- **Scale (rem / px):** 0.75/12 · 0.8125/13 · 0.875/14 (**body**) · 1/16 · 1.25/20 · 1.5/24 ·
  1.875/30 · 2.25/36 (KPI numbers, e.g. the balance card).
- **Weights:** 400 body · 500 medium (labels, nav, buttons) · 600 semibold (headings, KPI numbers) ·
  700 large display numbers only.

## 3. Spacing & layout

- **Spacing scale (px):** 2 · 4 · 8 · 12 · 16 · 20 · 24 · 32 · 40 · 48 (4px base).
- **Desktop shell:** dark **sidebar 248px** (collapsible to 72px icon-only), top bar ~64px, content
  max-width ~1200–1360 with 24–32px gutters.
- **Mobile shell (reference-first):** full-bleed dark canvas, sticky search, **rounded bottom nav
  pill** with the active icon in green; cards stack with 16px gaps.
- **Density:** spacious for dashboard/detail (card padding 20–24); denser for lists/ledgers (row 40
  comfortable / 32 compact, user toggle).

## 4. Radius, border & elevation

- **Radius:** hero/balance card `24`; cards/large surfaces `20`; inputs/buttons/menus `12`; dense
  table containers `10`; pills/avatars full.
- **Surfaces:** flat `--surface` fills + hairline `--border`; **no shadows-as-gradient**; a subtle
  soft shadow (`0 1px 2px rgba(0,0,0,.4)`) is allowed for lifted cards/menus. **No gradient fills.**
- **Focus ring:** 2px `--accent` + 2px offset.

## 5. Iconography

- **Lucide** (MIT), ~1.5px stroke, 20px default. Active nav item may use the filled/solid variant.
  **No emoji.** People/merchants use circular **avatar chips** (initials or image).

## 6. Motion

- Subtle & fast: 120–180ms ease for hovers/menus/toggles; respect `prefers-reduced-motion`. No
  parallax, no bouncing.

## 7. Core components

App shell (desktop sidebar + top bar with ⌘K; mobile bottom-nav pill), **BalanceCard** (large
tabular number + delta chip + account avatars), `Card`, `StatTile` (label/number/delta),
`StatusChip` (semantic: Cleared/Uncleared), `Button` (primary green w/ dark text · secondary outline
· ghost · danger), **TransactionRow** (avatar + title + time + signed amount in positive/negative),
**SpendBar** (segmented category spectrum), `DataTable` (dense, sortable, sticky header, tabular
nums, density toggle), `TransactionForm` (Income/Expense/Transfer), `Money` display (id-ID,
negatives in parens), charts (bar/line/donut, **solid** category-spectrum series, dark tooltip),
toasts, modals, command palette.

## 8. Token → Tailwind mapping

CSS custom properties are defined on `:root` (dark) and, when a light theme lands, on
`[data-theme="light"]`. Tailwind's theme extends to reference them (e.g.
`colors.accent.DEFAULT = 'var(--accent)'`), so one source drives utilities and component CSS. No
hardcoded hex in components — always token classes/vars.

## 9. Anti-patterns (explicitly banned)

Gradients (backgrounds, buttons, charts, text), glassmorphism/blur panels, emoji, neon glows, purple
accents, stock 3D illustrations, more than one accent color, non-tabular figures on money.
