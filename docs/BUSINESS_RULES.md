# Business Rules

Catalog of business rules with stable ids (`BR-*`). Rules must never live only in source code — they
are documented here and enforced in the domain layer with tests. Codes referenced by the API appear
in [ERROR_HANDLING.md](ERROR_HANDLING.md).

## Ledger

- **BR-001** A transaction `Amount` must be **greater than 0**. Direction is implied by `Type`
  (Income +, Expense −, Transfer moves).
- **BR-002** A **Transfer** must have a `ToAccountId`, and it must differ from `AccountId`.
- **BR-003** An **Income** or **Expense** must have `ToAccountId = null`.
- **BR-004** A transaction's `Date` may be in the past (backdating allowed) but not in the future
  beyond today (configurable; default: no future-dated transactions).
- **BR-005** A transaction's category must match its flow: an Expense uses a category whose `flow` is
  `Expense` or `Any`; Income uses `Income`/`Any`; Transfers use no category (or a `Transfer` one).
- **BR-006** The referenced `Account(s)` and `Category(ies)` must exist and be **active**.
- **BR-007** A transaction is **never physically deleted**. It is **voided** (soft-deleted); a voided
  transaction stops contributing to balances and reports.
- **BR-008** Editing a transaction re-derives all affected balances; there is no stored running
  balance to reconcile.
- **BR-009** `Status` is `Cleared` or `Uncleared`. Clearing does not change the amount or balance —
  it is a reconciliation marker only. (Both cleared and uncleared count toward derived balances,
  matching the workbook.)

## Accounts

- **BR-020** An account has an `OpeningBalance` (default 0) that anchors its derived balance.
- **BR-021** An account cannot be **deactivated** while referenced by any non-voided transaction.
- **BR-022** Account `balance` and per-month balances are **always derived** from the ledger — never
  stored as authoritative.
- **BR-023** Net worth = sum of derived balances over **active** accounts in the functional currency.

## Categories

- **BR-040** Categories form a three-level tree: `Budget` → `Category` → `Sub`. A `Category`'s parent
  is a `Budget`; a `Sub`'s parent is a `Category`.
- **BR-041** System/seeded categories (e.g. Income, Transfer, Adjustment) cannot be deleted; they may
  be renamed (the rename is shown verbatim; the seeded localized name only applies while unchanged).
- **BR-042** A category cannot be deactivated while referenced by a non-voided transaction or a
  budget line.

## Budget

- **BR-060** A `BudgetPeriod` is unique per `(year, month)`.
- **BR-061** A `BudgetLine` is unique per `(period, category)` and stores only `PlanAmount`.
- **BR-062** `Actual(period, category)` = Σ non-voided expense amounts for that category within the
  period; `Leftover = Plan − Actual`. Both are computed, never stored.

## Master Plan

- **BR-080** A Master Plan item's `TotalBudget = Price × Frequency` (computed).
- **BR-081** Sections are exactly **Investment / Needs / Wants**, each with a `target_percent`; the
  three targets should sum to 100% (default 40 / 50 / 10). A non-100 sum is a soft warning, not a
  hard error.

## Money & currency

- **BR-100** All amounts carry a `CurrencyCode`; the functional currency is **IDR**.
- **BR-101** Money is stored as `numeric(19,2)`; fractional values (e.g. interest) are preserved.
- **BR-102** Multi-currency transfers (workbook SGD/JPY/USD) are **out of MVP scope**; the schema
  reserves the fields.

## Data integrity

- **BR-120** Every mutation stamps audit fields (`CreatedBy/At`, `UpdatedBy/At`) and, on removal,
  `DeletedBy/At` + `IsDeleted`.
- **BR-121** Financial data is never physically deleted.
- **BR-122** Imports run as a **dry-run preview → commit**; a failed row does not partially commit.
