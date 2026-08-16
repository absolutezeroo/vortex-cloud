#!/usr/bin/env python3
"""
Purchasable-clothing scanner
============================
Writes ``Vortex.Database/Seeds/purchasable_clothing.sql``: which figure sets each clothing furni
hands over, and which sets are sold rather than free.

Why this exists
---------------
A clothing furni ("bind clothing") unlocks one or more avatar figure sets for the account that
redeems it. The hotel shipped 2 800-odd of them and no mapping at all, so redeeming one had nothing
to grant -- the furni resolved to the default floor logic and did nothing.

The mapping is not something an admin should type. It already exists in the shipped furnidata, in
each item's ``customparams``: a comma-separated list of figure set ids. The client reads it from
there itself, which is how it can draw the preview of the outfit before the server is involved.

THE FIELD THAT MATTERS IS ``specialtype``, NOT ``category``
----------------------------------------------------------
The client decides a furni is clothing with ``category - 23 == 0``, and that ``category`` is the
*numeric* one on its FurnitureData -- which the parser fills from furnidata's ``specialtype``
(argument 21 of the constructor), not from the ``category`` string beside it. The two disagree
badly here: only 588 items carry the string ``clothing`` while 2 802 carry ``specialtype`` 23, and
the string field is full of ``unknow`` (Habbo's own typo), ``unknown`` and ``other``. Reading the
string would have seeded a fifth of the set and silently dropped the rest.

The sellable list
-----------------
``figuredata.xml`` marks a set ``sellable="1"`` when it must be owned to be worn; the avatar editor
greys those out client-side by asking the inventory. That check is client-side only, so the same
list is seeded here for the server to enforce the same rule on a saved figure.

Usage
-----
    python tools/catalog_converter/scan_purchasable_clothing.py [assets_gamedata_dir]

Defaults to the local Laragon asset root. Re-run it whenever the hotel's furnidata or figuredata
changes; the output is idempotent (INSERT IGNORE) and name-scoped, so a trimmed furnidata simply
matches fewer definitions.
"""

import collections
import io
import json
import os
import re
import sys

DEFAULT_GAMEDATA = r"C:/Laragon/www/vortex-assets/gamedata"
OUT = os.path.join(
    os.path.dirname(os.path.abspath(__file__)),
    "..",
    "..",
    "Vortex.Database",
    "Seeds",
    "purchasable_clothing.sql",
)

# The client's numeric furni category for "this is a wearable set", read from furnidata's
# specialtype. See the module docstring: the string `category` field is not this.
CLOTHING_SPECIALTYPE = 23


def read_clothing(gamedata):
    """(classname, [figure set ids]) for every furni the client will treat as clothing."""
    path = os.path.join(gamedata, "furnidata_json.json")
    data = json.loads(io.open(path, encoding="utf-8").read())
    items = data["roomitemtypes"]["furnitype"]

    pairs = []
    skipped = 0

    for item in items:
        if item.get("specialtype") != CLOTHING_SPECIALTYPE:
            continue

        classname = str(item.get("classname") or "").strip()
        raw = str(item.get("customparams") or "").strip()

        if not classname or not raw:
            # A clothing furni with no set ids grants nothing; it would redeem into silence.
            skipped += 1
            continue

        ids = sorted({int(p) for p in (s.strip() for s in raw.split(",")) if p.isdigit()})

        if ids:
            pairs.append((classname, ids))
        else:
            skipped += 1

    return pairs, skipped


def read_sellable(gamedata):
    """The figure sets that must be owned to be worn."""
    path = os.path.join(gamedata, "figuredata.xml")
    xml = io.open(path, encoding="utf-8", errors="replace").read()

    return sorted(
        {
            int(set_id)
            for set_id, attrs in re.findall(r'<set id="(\d+)"([^>]*)>', xml)
            if 'sellable="1"' in attrs
        }
    )


def sql_literal(value):
    return "'" + value.replace("\\", "\\\\").replace("'", "''") + "'"


def chunks(seq, size):
    for i in range(0, len(seq), size):
        yield seq[i : i + size]


def main():
    gamedata = sys.argv[1] if len(sys.argv) > 1 else DEFAULT_GAMEDATA

    pairs, skipped = read_clothing(gamedata)
    sellable = read_sellable(gamedata)

    rows = [(name, set_id) for name, ids in pairs for set_id in ids]
    distinct_sets = {set_id for _, set_id in rows}
    duplicates = [n for n, c in collections.Counter(n for n, _ in pairs).items() if c > 1]

    out = io.open(OUT, "w", encoding="utf-8", newline="\n")
    w = out.write

    w(
        "-- Clothing furni and the avatar figure sets they hand over, taken from the shipped assets.\n"
        "--\n"
        "-- Regenerate with tools/catalog_converter/scan_purchasable_clothing.py, which reads\n"
        "-- furnidata_json.json and figuredata.xml. Do not hand-edit: the mapping is the hotel's\n"
        "-- own data, and there are thousands of rows.\n"
        "--\n"
        "-- The furni are selected by furnidata's `specialtype` = 23, which is what the client's\n"
        "-- numeric furni category actually comes from. The `category` string beside it disagrees --\n"
        "-- only 588 items say 'clothing' where 2 802 carry specialtype 23, the rest saying 'unknow'\n"
        "-- (Habbo's own typo), 'unknown' or 'other'. Reading the string would have seeded a fifth of\n"
        "-- the set and silently dropped the rest.\n"
        "--\n"
        "-- Matched by classname, which is NOT unique in furniture_definitions: a classname shared by\n"
        "-- several definitions grants its sets through every one of them, which is the correct\n"
        "-- reading -- each is the same furni to the client.\n"
        "--\n"
        f"-- {len(pairs)} clothing furni, {len(rows)} furni/set pairs, {len(distinct_sets)} distinct sets.\n"
        f"-- {skipped} carried specialtype 23 with no usable customparams and are left out: they would\n"
        "-- redeem into silence.\n"
        f"-- {len(duplicates)} classnames appear more than once in the furnidata itself.\n"
        "\n"
        "CREATE TEMPORARY TABLE `_clothing_seed` (\n"
        "  `name` VARCHAR(128) NOT NULL,\n"
        "  `figure_set_id` INT NOT NULL,\n"
        "  KEY `ix_name` (`name`)\n"
        ") ENGINE=MEMORY;\n\n"
    )

    for batch in chunks(rows, 500):
        w("INSERT INTO `_clothing_seed` (`name`, `figure_set_id`) VALUES\n")
        w(",\n".join(f"  ({sql_literal(n)}, {s})" for n, s in batch))
        w(";\n\n")

    w(
        "INSERT IGNORE INTO `furniture_purchasable_clothing`\n"
        "  (`furniture_definition_id`, `figure_set_id`, `created_at`, `updated_at`)\n"
        "SELECT `d`.`id`, `s`.`figure_set_id`, UTC_TIMESTAMP(), UTC_TIMESTAMP()\n"
        "  FROM `_clothing_seed` AS `s`\n"
        "  JOIN `furniture_definitions` AS `d` ON `d`.`name` = `s`.`name`\n"
        " WHERE `d`.`deleted_at` IS NULL;\n\n"
        "DROP TEMPORARY TABLE `_clothing_seed`;\n\n"
        f"-- {len(sellable)} figure sets are marked sellable in figuredata: they have to be owned to\n"
        "-- be worn. The avatar editor already hides the ones the player lacks, but that is the\n"
        "-- client's own check -- this list is what lets the server refuse a forged figure.\n"
    )

    for batch in chunks(sellable, 500):
        w("INSERT IGNORE INTO `figure_sellable_sets` (`figure_set_id`, `created_at`, `updated_at`)\nVALUES\n")
        w(",\n".join(f"  ({s}, UTC_TIMESTAMP(), UTC_TIMESTAMP())" for s in batch))
        w(";\n\n")

    out.close()

    print(f"{len(pairs)} clothing furni -> {len(rows)} pairs, {len(distinct_sets)} distinct sets")
    print(f"{len(sellable)} sellable sets")
    print(f"{skipped} skipped (specialtype 23, no usable customparams)")
    print("written:", os.path.normpath(OUT))


if __name__ == "__main__":
    main()
