---
name: sql-fix
description: Fix wrong or missing rows with a reviewable SQL script in scripts/sql/ instead of building a seeder. Use for one-off data corrections.
disable-model-invocation: true
---

# Data fix

## The decision, first

| Symptom | Fix |
|---|---|
| Rows are wrong **once** — a hole in the catalogue, a missing `currency_types` row, a bad backfill | A script in `scripts/sql/`, run once. **Default.** |
| The data must be correct on **every boot** — a fresh install must come up with it | Then, and only then, a hosted seeder |

A hosted seeder for a one-off hole is overkill: it adds startup work, a code path to maintain, and a
migration story, to do something a reviewable `.sql` file does once and then stops doing. When in
doubt, write the SQL.

## Recipe

1. **Measure first.** A `SELECT` that counts exactly the rows the fix will touch. Paste the count
   into the conversation before writing any `UPDATE`/`INSERT`. If the count surprises you, the
   diagnosis is wrong, not the query.
2. **Write it to `scripts/sql/<subject>.sql`.** Existing scripts are the format reference
   (`wired_catalog_fill.sql` is the fullest). A header comment states what it does and what it
   deliberately does not touch — `wired_catalog_fill.sql` opens with "Additif : ne modifie, ne
   déplace et ne réaffiche aucune offre existante", which is exactly the sentence that makes a data
   script reviewable.
3. **Make it idempotent.** Guard inserts with `NOT EXISTS`, scope updates with a `WHERE` that stops
   matching once applied. Running it twice must be a no-op, because it will be run twice.
4. **Hand it to the user to run.** Do not execute it yourself unless asked. Give the command and the
   expected row count.

```bash
/c/laragon/bin/mysql/mysql-8.4.3-winx64/bin/mysql.exe -h127.0.0.1 -uroot -padmin turbo \
  < scripts/sql/<subject>.sql
```

## Traps

- **A wallet credit is a silent no-op if the currency has no `currency_types` row** — and the
  dashboard reports success anyway. Check that table before diagnosing a "grant didn't work".
- **All 26 admin deletes are hard deletes.** Take a `mysqldump` before any destructive script.
  Do not switch anything to soft-delete: the catalogue runtime never filters `DeletedAt`.
- `furniture_definitions.name` is **not unique** (3533 duplicates, by design — the client's own
  furnidata ships duplicates). Never key a join or a `ToDictionary` on it.
