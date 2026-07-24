# Brand (placeholder)

> **Placeholder identity.** The final logo will be designed later; this exists so the app shell,
> favicon, and login screen have a consistent, non-generic mark to build against. Keep it simple and
> easy to swap.

## Mark

A **stacked-coins / accumulating-bars** mark (white/near-black, rounded) inside a rounded-square tile
in the brand green `#35D07A`. The rising, stacking form reads as *saving up / accumulating* —
matching the name **Tameru** (溜める, "to save up") — without a literal dollar sign or generic letter.
Renders without any font dependency.

- File: `brand/logo-mark.svg` — 40×40, rounded-square (radius 11), green tile + dark motif.
- App-icon / favicon source. For small favicons (16/32px) keep just the tile + motif.
- **Flat fill only — no gradient** (per design language).

## Wordmark / lockup

“Ta**meru**” — the **“meru”** set in brand green `#35D07A`, the rest in `--text`. Inter, 700, slight
negative tracking.

- File: `brand/logo-lockup.svg` — mark + wordmark, for the sidebar header, login, and docs.

## Variants

- **Dark background / sidebar (default):** wordmark in `#F4F5F6`; green tile + dark motif unchanged.
- **Light background (future light theme):** wordmark in `#131415`; tile unchanged. At build time the
  wordmark uses `currentColor` so it adapts to the theme automatically.

## Usage

- Clear space ≥ the tile's corner radius on all sides; don't recolor the tile, stretch, add effects,
  or apply gradients. Minimum mark size 24px.
- The green tile is the only place the accent appears "as a fill" at rest besides primary actions —
  keep it special.
