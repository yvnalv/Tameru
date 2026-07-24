# Glossary

Ubiquitous language for Tameru. Use these terms consistently in code, docs, and UI.

| Term | Meaning |
|---|---|
| **Account** | A money container: cash, bank, e-wallet, investment, or blocked. Has an opening balance; its live balance is derived. |
| **Account Group** | A label grouping accounts for roll-ups (Saving, Investment, Family, …). From the workbook's account tags. |
| **Transaction** | A single ledger entry. One of three **Types**: Income, Expense, Transfer. |
| **Income** | A transaction that increases an account's balance. |
| **Expense** | A transaction that decreases an account's balance. |
| **Transfer** | A transaction that moves money from one account (`Account`) to another (`To Account`). |
| **Status** | `Cleared` or `Uncleared` — a reconciliation marker; does not affect the derived balance. |
| **Category** | A classification node. Three **Levels**: Budget → Category → Sub. |
| **Budget (level)** | Top envelope: Investment, Needs, Wants, Income. |
| **Sub** | The most specific category level (e.g. Household under Wife). |
| **Budget (module)** | Monthly plan: `Plan` per category vs `Actual` (from the ledger) = `Leftover`. |
| **Master Plan** | Allocation planner: Investment/Needs/Wants items (`Price × Frequency = Total`) with a target split (default 40/50/10). |
| **Balance** | Derived amount of an account = opening balance + ledger movements up to a date. |
| **Net Worth** | Sum of derived balances over active accounts, in the functional currency. |
| **Functional Currency** | The single currency all reporting is expressed in — **IDR**. |
| **Cleared / Uncleared** | Whether the owner has confirmed the transaction settled at the source. |
| **Void** | Soft-delete of a transaction; it stops contributing to balances/reports but is retained. |
| **Read Model** | A cached projection used by Reporting, rebuildable from ledger events. |
| **THR** | *Tunjangan Hari Raya* — Indonesian religious-holiday allowance (Work phase). |
| **PPN / PPh / Levy** | Indonesian taxes/fees on stock trades (Investments phase). |
| **RDN** | *Rekening Dana Nasabah* — an investor's brokerage cash account. |
| **Ledger** | The set of all transactions; the single source of truth. |
| **Derived** | Computed from the ledger on demand (or cached), never stored as authoritative truth. |
