# scripts

Developer utilities. Not part of the application or the deployed image.

## seed_demo.py

Seeds a lot of realistic demo data (5 accounts + hundreds of income/expense/transfer transactions
across ~10 months) into a running API so the UI has something to show. Local development only —
never run it against real data.

```bash
# With the local Docker stack up (docker compose up -d):
python scripts/seed_demo.py
```

It logs in as the seeded owner (`owner@tameru.local` / `ChangeMe!123`), targets `http://localhost:8090`,
reuses accounts by name (safe to re-run — it adds more transactions), and prints the resulting net
worth. Wipe everything with `docker compose down -v` and re-seed for a clean slate.
