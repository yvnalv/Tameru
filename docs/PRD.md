# Product Requirements (PRD)

## Problem

For years, personal finances have been tracked in a large Excel workbook
(`Financial Projection (Indonesia)`). It works, but it is fragile (formulas break, sheets sprawl to
35 tabs), hard to use on mobile, and slow to answer simple questions ("how much did I spend on Food
this month?", "am I on budget?", "what's my net worth?"). Tameru replaces the workbook with a
purpose-built app that keeps the same mental model but adds structure, safety, and good UX.

## Goals

- Faithfully reproduce the workbook's **cashflow model**: Income, Expenses, Transfers → Accounts.
- Make **budgeting** (monthly Plan/Actual/Leftover) and **allocation** (Master Plan
  Investment/Needs/Wants) first-class.
- Provide fast, trustworthy **dashboards** (net worth, monthly cashflow, category breakdown).
- Be pleasant to use daily — a dark, calm, mobile-friendly fintech feel.
- Be **bilingual** (EN + ID) and Indonesia-first (IDR, `id-ID`).

## Non-goals (MVP)

- Multi-user / sharing / multi-tenant.
- Double-entry accounting, tax reporting, invoicing, inventory.
- Bank sync / open-banking imports (manual + spreadsheet import only, later).
- Investments, payroll, and long-term planning modules (later phases).

## Target user

One person (the author), managing personal money plus a small side-business ("Shirt Lab") tracked as
ordinary accounts and categories — not as a separate company.

## MVP scope

Confirmed groups (ADR-0003):

1. **Core ledger + Accounts**
   - Record Income, Expense, and Transfer transactions with: date, title, amount, account (+ target
     account for transfers), category (Budget→Category→Sub), status (Cleared/Uncleared), description.
   - Manage Accounts and Account Groups; view derived balances (current + per month).
2. **Budget + Master Plan**
   - Monthly Budget: set Plan per category; see Actual (from ledger) and Leftover.
   - Master Plan: Investment / Needs / Wants items (Price × Frequency = Total) with the 40/50/10
     target split.
3. **Dashboards & Overview**
   - Net worth, this-month income vs expense, cashflow trend, category breakdown (daily/monthly),
     yearly overview.

## Key user stories

- *As the owner, I add an expense* in a few taps: amount, category, account, done.
- *I record a transfer* between two of my accounts and both balances update.
- *I mark a transaction Cleared* once it settles at the bank.
- *I set a monthly budget* per category and watch Actual approach Plan.
- *I plan my allocation* with the Master Plan and see if my Needs exceed 50%.
- *I open the dashboard* and immediately see net worth and where this month's money went.
- *I switch the language* to Bahasa Indonesia and every label follows.

## Success criteria

- All recurring workbook workflows are doable in the app without opening Excel.
- Balances derived by the app match the workbook for an imported historical dataset.
- Adding a transaction takes < 10 seconds on mobile.
- Every screen renders correctly in both EN and ID.

## Constraints

- Single functional currency IDR (multi-currency reserved).
- Ledger is the source of truth; no stored balance is authoritative.
- Soft delete only; full audit history.
