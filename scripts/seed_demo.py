"""Seed a lot of realistic demo data into the running Tameru API (docker :8090).
Idempotent-ish: reuses accounts by name if they already exist. Safe to re-run (adds more txns)."""
import json, random, sys, urllib.request, datetime as dt

BASE = "http://localhost:8090/api/v1"
random.seed(42)

def req(method, path, token=None, body=None):
    data = json.dumps(body).encode() if body is not None else None
    r = urllib.request.Request(BASE + path, data=data, method=method)
    r.add_header("Content-Type", "application/json")
    if token:
        r.add_header("Authorization", "Bearer " + token)
    try:
        with urllib.request.urlopen(r) as resp:
            return json.load(resp)
    except urllib.error.HTTPError as e:
        return json.load(e)

# --- login -----------------------------------------------------------------
token = req("POST", "/auth/login", body={"email": "owner@tameru.local", "password": "ChangeMe!123"})["data"]["accessToken"]

# --- categories ------------------------------------------------------------
def flatten(nodes, out):
    for n in nodes:
        out.append(n); flatten(n.get("children") or [], out)
cats = []
flatten(req("GET", "/categories", token)["data"], cats)
cat = {c["name"]: c["id"] for c in cats}
budget = {c["name"]: c["id"] for c in cats if c["level"] == "Budget"}

# --- accounts (reuse by name) ---------------------------------------------
existing = {a["name"]: a["id"] for a in req("GET", "/accounts", token)["data"]}
def ensure_account(name, atype, opening, sort):
    if name in existing:
        return existing[name]
    res = req("POST", "/accounts", token, {
        "name": name, "type": atype, "openingBalance": opening,
        "currencyCode": "IDR", "sortOrder": sort})
    if not res.get("success"):
        print("account fail", name, res); sys.exit(1)
    return res["data"]["id"]

acc = {
    "BCA":            ensure_account("BCA", "Bank", 15_000_000, 1),
    "Cash Wallet":    ensure_account("Cash Wallet", "Cash", 500_000, 2),
    "GoPay":          ensure_account("GoPay", "EWallet", 250_000, 3),
    "Jenius Savings": ensure_account("Jenius Savings", "Bank", 8_000_000, 4),
    "Ajaib RDN":      ensure_account("Ajaib RDN", "Investment", 10_000_000, 5),
}

# --- transaction generation ------------------------------------------------
# category -> (budget name, [account names], (min, max), [titles], per-month count range)
EXPENSES = {
    "Food":           ("Needs", ["Cash Wallet", "GoPay", "BCA"], (18_000, 180_000),
                        ["Breakfast", "Lunch", "Dinner", "Groceries", "Coffee", "Snacks"], (12, 20)),
    "Transportation": ("Needs", ["GoPay", "Cash Wallet", "BCA"], (12_000, 120_000),
                        ["Fuel", "Grab ride", "Toll", "Parking", "Train"], (6, 12)),
    "Internet":       ("Needs", ["BCA"], (300_000, 400_000),
                        ["Internet bill", "Mobile data"], (1, 2)),
    "Personal":       ("Needs", ["BCA", "Cash Wallet"], (50_000, 400_000),
                        ["Pharmacy", "Haircut", "Clothes", "Toiletries"], (2, 4)),
    "Entertainment":  ("Wants", ["BCA", "GoPay"], (40_000, 350_000),
                        ["Movie", "Streaming", "Game", "Dining out", "Concert"], (3, 6)),
}
INCOME_TITLES = ["Salary", "Freelance", "Bonus"]

created = 0
def post_txn(body):
    global created
    res = req("POST", "/transactions", token, body)
    if not res.get("success"):
        print("txn fail", res, body); sys.exit(1)
    created += 1

today = dt.date(2026, 7, 24)
start = dt.date(2025, 10, 1)  # 10 months of history
month = dt.date(start.year, start.month, 1)
while month <= today:
    y, m = month.year, month.month
    def d(day):
        day = min(day, 28)
        return dt.date(y, m, day).isoformat()

    # Salary income to BCA on the 25th
    post_txn({"type": "Income", "date": d(25), "title": "Salary",
              "amount": 12_000_000 + random.randint(0, 8) * 100_000,
              "accountId": acc["BCA"], "budgetCategoryId": budget["Income"],
              "status": "Cleared", "description": "Monthly salary"})
    if random.random() < 0.4:
        post_txn({"type": "Income", "date": d(random.randint(5, 20)),
                  "title": random.choice(INCOME_TITLES[1:]),
                  "amount": random.randint(5, 25) * 100_000,
                  "accountId": acc["BCA"], "budgetCategoryId": budget["Income"], "status": "Cleared"})

    # Monthly investing transfer BCA -> Ajaib RDN
    post_txn({"type": "Transfer", "date": d(26), "title": "Monthly investing",
              "amount": 2_000_000, "accountId": acc["BCA"], "toAccountId": acc["Ajaib RDN"],
              "status": "Cleared"})
    # Savings transfer BCA -> Jenius
    post_txn({"type": "Transfer", "date": d(26), "title": "Savings",
              "amount": 1_500_000, "accountId": acc["BCA"], "toAccountId": acc["Jenius Savings"],
              "status": "Cleared"})
    # A couple of top-ups BCA -> GoPay / Cash
    for _ in range(random.randint(1, 3)):
        dst = random.choice(["GoPay", "Cash Wallet"])
        post_txn({"type": "Transfer", "date": d(random.randint(1, 28)), "title": f"Top up {dst}",
                  "amount": random.randint(2, 8) * 100_000, "accountId": acc["BCA"],
                  "toAccountId": acc[dst], "status": "Cleared"})

    # Expenses
    is_current = (y == today.year and m == today.month)
    for cname, (bname, accts, (lo, hi), titles, (cmin, cmax)) in EXPENSES.items():
        n = random.randint(cmin, cmax)
        for _ in range(n):
            day = random.randint(1, today.day if is_current else 28)
            amount = random.randint(lo // 1000, hi // 1000) * 1000
            status = "Cleared" if not (is_current and random.random() < 0.3) else "Uncleared"
            post_txn({"type": "Expense", "date": d(day), "title": random.choice(titles),
                      "amount": amount, "accountId": acc[random.choice(accts)],
                      "budgetCategoryId": budget[bname], "categoryId": cat[cname],
                      "status": status})

    # advance month
    month = dt.date(y + (m // 12), (m % 12) + 1, 1)

print(f"created {created} transactions across {len(acc)} accounts")
nw = req("GET", "/reports/net-worth", token)["data"]
print(f"net worth now: Rp {nw['total']:,.0f} across {len(nw['accounts'])} accounts")
