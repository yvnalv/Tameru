# Coding Standards

Conventions for Tameru's backend (C#/.NET 8) and frontend (Vue 3 + TypeScript). Goal: consistent,
readable, testable code that matches the surrounding style.

## General

- Match the surrounding code's style, naming, and comment density.
- Small, focused units. Avoid God classes, massive services, circular dependencies, shared mutable
  state, and business logic in static helpers.
- Prefer SOLID, dependency injection, and Clean Architecture boundaries.
- No secrets in code. No `TODO`-only PRs — either do it or track it in STATUS/ROADMAP.

## C# / .NET

- **Naming:** `PascalCase` types/methods/properties; `camelCase` locals/parameters; `_camelCase`
  private fields; `IPascalCase` interfaces. Async methods end with `Async`.
- **Nullability:** nullable reference types **on**; no `!` null-forgiving without justification.
- **Domain purity:** Domain layer references no EF Core, ASP.NET, or infrastructure types. Use value
  objects (`Money`, `CurrencyCode`) and guard clauses; throw typed `DomainRuleException(code)`.
- **Application:** one class per use case (command/query + handler); validate inputs; return
  `Result`/DTOs, not entities.
- **Persistence:** EF Core configurations in Infrastructure (`IEntityTypeConfiguration<T>`); no
  business logic in DbContext; repositories only where they add value.
- **Money:** always `decimal`; never `double`/`float` for money. Compare/round explicitly.
- **Enums:** map to stable stored values; render via i18n on the client, never `ToString()` to the
  user.
- **Async:** async all the way; pass `CancellationToken`; avoid `.Result`/`.Wait()`.
- **Errors:** throw for exceptional/rule violations; don't swallow exceptions.
- **Formatting:** `dotnet format`; 4-space indent; file-scoped namespaces; `var` when the type is
  obvious.

## TypeScript / Vue

- **Vue 3** `<script setup lang="ts">`, Composition API. Components `PascalCase.vue`.
- **TypeScript strict**; no `any` (use precise DTO types in `types/`).
- **State** in Pinia stores; API access via typed modules in `lib/`; never call `axios` from
  components directly.
- **i18n:** no hardcoded user-facing strings — everything through `t('…')`; keep `en.ts` and `id.ts`
  structurally identical.
- **Money/numbers:** format via `formatMoney`/`formatNumber` (`id-ID`, negatives in parentheses) with
  the tabular-figures class; never format inline.
- **Styling:** Tailwind utilities bound to design tokens; no hardcoded hex — use token classes/CSS
  vars (see [frontend/DESIGN_LANGUAGE.md](frontend/DESIGN_LANGUAGE.md)). No gradients.
- **Icons:** Lucide only. No emoji in UI.

## Tests

- Test names describe behavior: `Method_State_Expectation`. Arrange/Act/Assert.
- Money-critical logic (balances, transfers, budget actuals) must have unit tests. See
  [TESTING.md](TESTING.md).

## Commits & docs

- Conventional, imperative commit subjects (e.g. `feat(ledger): add transfer validation`).
- A change that touches schema/architecture/API/rules updates the matching doc **in the same change**.
- Update `CHANGELOG.md` (`CHG-*`) as the final step of a task.
- **Author is Yovan Alvianto only** — no AI co-author trailers anywhere.
