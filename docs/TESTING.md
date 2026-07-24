# Testing

Testing is part of the definition of done. Priority is the money-critical logic — anything that
computes or moves balances.

## Test types

| Type | Framework | Scope |
|---|---|---|
| **Unit** | xUnit + FluentAssertions | Domain rules, use-case handlers, calculations. No DB, no HTTP. |
| **Integration** | xUnit + Testcontainers (PostgreSQL) | API + EF Core against a real Postgres. |
| **Architecture** | NetArchTest | Module-boundary fitness (no cross-module internal references). |
| **Frontend unit** | Vitest | Formatters, stores, composables. |

Test projects mirror modules:

```
tests/
├── Tameru.Accounts.UnitTests
├── Tameru.Ledger.UnitTests
├── Tameru.Budgeting.UnitTests
├── Tameru.Reporting.UnitTests
├── Tameru.Identity.UnitTests
├── Tameru.IntegrationTests
└── Tameru.ArchitectureTests
```

## High-priority coverage (must have unit tests)

1. **Balance derivation** (BR-022, DATABASE §Derived balance):
   - opening balance only → balance equals opening.
   - income adds, expense subtracts, up to a given date.
   - transfer subtracts from source and adds to destination.
   - voided transactions are excluded.
   - date cutoff (`date ≤ d`) respected.
2. **Transfer integrity** (BR-002/003): source ≠ destination; income/expense have no `ToAccount`.
3. **Amount validation** (BR-001): amount must be > 0.
4. **Category flow matching** (BR-005): expense rejects an income-only category.
5. **Account deactivation guard** (BR-021): blocked while referenced by a non-voided transaction.
6. **Budget actual/leftover** (BR-062): actual = Σ expenses for category in period; leftover = plan −
   actual; voided excluded.
7. **Master plan totals** (BR-080): total = price × frequency; section rollups.
8. **Net worth** (BR-023): sum over active accounts only.

## Example (illustrative)

```csharp
[Fact]
public void Balance_WithTransfer_MovesBetweenAccounts()
{
    var a = Account.Open("BCA", openingBalance: 100_000m);
    var b = Account.Open("Cash", openingBalance: 0m);
    var t = Transaction.Transfer(from: a.Id, to: b.Id, amount: 30_000m, date: Today);

    var balA = BalanceCalculator.For(a, new[]{ t }, asOf: Today);
    var balB = BalanceCalculator.For(b, new[]{ t }, asOf: Today);

    balA.Should().Be(70_000m);
    balB.Should().Be(30_000m);
}
```

## Conventions

- Deterministic: inject `IClock`; never use `DateTime.Now` in logic.
- Money literals use `decimal` (`_m` suffix). Never `double` in assertions.
- Integration tests spin up Postgres via Testcontainers, run migrations, seed, then assert through
  the API envelope.
- Frontend: unit-test `formatMoney` (id-ID, negatives in parens), stores, and the auth guard.

## Running

```bash
dotnet test                       # all backend tests
dotnet test tests/Tameru.Ledger.UnitTests
cd frontend && npm run test       # vitest
```

## CI

GitHub Actions runs: `dotnet build` → `dotnet test` (unit + architecture always; integration when
Docker is available) → frontend `npm run build` + `npm run test`. A red suite blocks merge.

## Definition of done

A feature is done when: behavior is covered by tests, the suite is green, docs touched are updated,
and `CHANGELOG.md` has a new `CHG-*` entry.
