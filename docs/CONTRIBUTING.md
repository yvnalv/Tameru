# Contributing

Tameru is authored and maintained by **Yovan Alvianto** (sole author). This document is the working
agreement for any contributor — human or AI assistant.

## Authorship rule (important)

**All work is attributed to Yovan Alvianto only.** Do **not** add "Claude", "Anthropic", any AI
co-author, a `Co-Authored-By` trailer, or a "Generated with" line to commits, pull requests,
CHANGELOG entries, or documentation. Git `user.name` / `user.email` stay set to the author.

## Before you start

1. Read [`../CLAUDE.md`](../CLAUDE.md) fully — it overrides default behavior and is the source of
   truth.
2. Skim [STATUS.md](STATUS.md) for what's next and [DECISIONS.md](DECISIONS.md) for why things are
   the way they are.
3. Read the design doc for your area (see the [docs index](README.md)).

## Workflow

1. **Branch** off `main` (`feat/…`, `fix/…`, `docs/…`, `chore/…`). Do not commit directly to `main`.
2. **Implement** following [CODING_STANDARDS.md](CODING_STANDARDS.md); keep modules within their
   boundaries.
3. **Test** — add/adjust unit tests (money-critical logic is mandatory); run `dotnet test` and
   frontend `npm run test`. See [TESTING.md](TESTING.md).
4. **Update docs in the same change** — schema → DATABASE.md, endpoints → API_SPEC.md, rules →
   BUSINESS_RULES.md, decisions → DECISIONS.md, i18n → both `en.ts` and `id.ts`.
5. **Update `CHANGELOG.md`** — add the next `CHG-*` entry at the top (UTC timestamp). This is the
   final step; a task is not done without it.
6. **Open a PR** with a clear description. CI must be green (build + tests) before merge.

## Commit conventions

- Imperative, scoped subjects: `feat(ledger): validate transfer accounts`,
  `docs(database): add derived-balance formula`.
- Keep commits focused; avoid mixing refactors with features.

## Definition of done

Behavior is implemented and tested, the full suite is green, all touched docs are updated, both
locale dictionaries are in sync, and `CHANGELOG.md` has a new entry — with **no AI attribution**.

## Language

- Documentation and code identifiers: **English**.
- UI strings: **English + Bahasa Indonesia**, always through i18n (`CLAUDE.md` → Internationalization).
