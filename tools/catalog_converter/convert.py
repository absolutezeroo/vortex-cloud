#!/usr/bin/env python3
"""
Arcturus → Turbo catalog converter
=====================================
Input  (./input/):
    items_base.sql       → output/furniture_definitions.sql
    catalog_pages.sql    → output/catalog_pages.sql
    catalog_items.sql    → output/catalog_offers.sql
                         → output/catalog_products.sql

Run: python convert.py
"""

import re
import json
import os
import sys
from pathlib import Path

# ---------------------------------------------------------------------------
# Paths
# ---------------------------------------------------------------------------
BASE_DIR = Path(__file__).parent
INPUT_DIR = BASE_DIR / "input"
OUTPUT_DIR = BASE_DIR / "output"
OUTPUT_DIR.mkdir(exist_ok=True)

# ---------------------------------------------------------------------------
# Layout mapping  Arcturus string → Turbo wire string
# ---------------------------------------------------------------------------
LAYOUT_MAP: dict[str, str] = {
    "default_3x3": "default_3x3",
    "default_3x3_color_grouping": "default_3x3",
    "club_buy": "club_buy",
    "club_gift": "club_gifts",
    "frontpage": "frontpage4",
    "frontpage_featured": "frontpage_featured",
    "spaces": "spaces_new",
    "spaces_new": "spaces_new",
    "recycler": "recycler",
    "recycler_info": "recycler_info",
    "recycler_prizes": "recycler_prizes",
    "trophies": "trophies",
    "plasto": "default_3x3",
    "marketplace": "marketplace",
    "marketplace_own_items": "marketplace_own_items",
    "soundmachine": "soundmachine",
    "guilds": "guild_frontpage",
    "guild_furni": "guild_custom_furni",
    "info_duckets": "info_duckets",
    "info_rentables": "info_rentables",
    "info_pets": "default_3x3",
    "roomads": "roomads",
    "single_bundle": "single_bundle",
    "sold_ltd_items": "default_3x3",
    "badge_display": "badge_display",
    "bots": "default_3x3",
    "pets": "pets",
    "pets2": "pets2",
    "pets3": "pets3",
    "productpage1": "default_3x3",
    "room_bundle": "single_bundle",
    "recent_purchases": "default_3x3",
    "guild_forum": "guild_forum",
    "vip_buy": "vip_buy",
    "info_loyalty": "info_loyalty",
    "loyalty_vip_buy": "loyalty_vip_buy",
    "collectibles": "default_3x3",
    "petcustomization": "petcustomization",
}

# ---------------------------------------------------------------------------
# Arcturus items_base.type → Turbo ProductType int
# ---------------------------------------------------------------------------
PRODUCT_TYPE_MAP: dict[str, int] = {
    "s": 0,  # Floor
    "i": 1,  # Wall
    "e": 2,  # Effect
    "b": 3,  # Badge
    "r": 4,  # Robot
}

# ---------------------------------------------------------------------------
# FurnitureCategory based on (type, interaction_type)
# Default=1, WallPaper=2, Floor=3, Landscape=4, PostIt=5, Poster=6
# ---------------------------------------------------------------------------
def get_furni_category(item_type: str, interaction_type: str) -> int:
    if item_type == "i":
        it = interaction_type.lower()
        if it == "wallpaper":   return 2
        if it == "floor":       return 3
        if it == "landscape":   return 4
        if it == "postit":      return 5
        if it == "poster":      return 6
    return 1


def get_logic(interaction_type: str) -> str:
    return "none" if interaction_type in ("default", "") else interaction_type


# ---------------------------------------------------------------------------
# SQL helpers
# ---------------------------------------------------------------------------
def sql_str(value) -> str:
    """Escape and quote a value for MySQL INSERT."""
    if value is None:
        return "NULL"
    if isinstance(value, bool):
        return "1" if value else "0"
    if isinstance(value, (int, float)):
        return str(value)
    s = str(value)
    s = s.replace("\\", "\\\\")
    s = s.replace("'", "\\'")
    s = s.replace("\n", "\\n")
    s = s.replace("\r", "\\r")
    s = s.replace("\0", "\\0")
    return f"'{s}'"


def sql_bool(value) -> str:
    return "1" if value else "0"


def json_array(items: list) -> str:
    """Return a JSON array string, or NULL if the list is empty."""
    cleaned = [str(x) for x in items if x is not None and str(x).strip() != ""]
    if not cleaned:
        return "NULL"
    return sql_str(json.dumps(cleaned, ensure_ascii=False))


# ---------------------------------------------------------------------------
# MySQL INSERT parser
# ---------------------------------------------------------------------------
def parse_insert_values(line: str) -> list | None:
    """
    Extract the column values from one MySQL INSERT line.
    Returns a list of Python values (str, int, float, None).
    """
    m = re.search(r"VALUES\s*\((.+)\)\s*;?\s*$", line, re.IGNORECASE | re.DOTALL)
    if not m:
        return None

    raw = m.group(1)
    values: list = []
    i = 0
    n = len(raw)

    while i < n:
        # skip whitespace and commas between tokens
        while i < n and (raw[i].isspace() or raw[i] == ","):
            i += 1
        if i >= n:
            break

        if raw[i] == "'":
            # quoted string
            i += 1
            buf: list[str] = []
            while i < n:
                c = raw[i]
                if c == "\\" and i + 1 < n:
                    nxt = raw[i + 1]
                    esc = {"n": "\n", "t": "\t", "r": "\r", "\\": "\\", "'": "'", '"': '"', "0": "\0"}
                    buf.append(esc.get(nxt, nxt))
                    i += 2
                elif c == "'" and i + 1 < n and raw[i + 1] == "'":
                    buf.append("'")
                    i += 2
                elif c == "'":
                    i += 1
                    break
                else:
                    buf.append(c)
                    i += 1
            values.append("".join(buf))
        elif raw[i : i + 4].upper() == "NULL":
            values.append(None)
            i += 4
        else:
            end = i
            depth = 0
            while end < n:
                if raw[end] == "(":
                    depth += 1
                elif raw[end] == ")":
                    if depth == 0:
                        break
                    depth -= 1
                elif raw[end] == "," and depth == 0:
                    break
                end += 1
            token = raw[i:end].strip()
            if token:
                if "." in token:
                    try:
                        values.append(float(token))
                    except ValueError:
                        values.append(token)
                else:
                    try:
                        values.append(int(token))
                    except ValueError:
                        values.append(token)
            i = end

    return values


def iter_inserts(sql_file: Path) -> list[list]:
    """Yield parsed value lists for every INSERT line in an SQL file."""
    rows: list[list] = []
    with open(sql_file, encoding="utf-8", errors="replace") as f:
        for line in f:
            line = line.strip()
            if line.upper().startswith("INSERT INTO"):
                row = parse_insert_values(line)
                if row is not None:
                    rows.append(row)
    return rows


# ---------------------------------------------------------------------------
# 1. items_base → furniture_definitions
# ---------------------------------------------------------------------------
def convert_items_base(src: Path, dst: Path) -> tuple[dict[int, dict], dict[int, int]]:
    """
    Returns:
      lookup  – items_base.id -> metadata (product_type, allow_gift, ...)
      id_remap – items_base.id → canonical id when (sprite_id, type, category)
                 clashes with an already-emitted row.  Use remap[id] in
                 catalog_products.definition_id.
    """
    rows = iter_inserts(src)
    lookup:   dict[int, dict] = {}
    id_remap: dict[int, int]  = {}          # duplicate id → winning id
    seen_key: dict[tuple, int] = {}         # (sprite_id, product_type, category) → first id

    lines: list[str] = [
        "-- furniture_definitions converted from Arcturus items_base",
        "-- Generated by tools/catalog_converter/convert.py",
        "-- Rows with duplicate (sprite_id, type, category) are merged;",
        "-- catalog_products.definition_id is remapped accordingly.",
        "",
        "SET NAMES utf8mb4;",
        "SET FOREIGN_KEY_CHECKS = 0;",
        "",
    ]

    written = 0
    dupes   = 0

    for row in rows:
        if len(row) < 27:
            print(f"  [WARN] items_base row too short ({len(row)} cols), skipping", file=sys.stderr)
            continue

        (
            ib_id, sprite_id, item_name, public_name,
            width, length, stack_height,
            allow_stack, allow_sit, allow_lay, allow_walk,
            allow_gift, allow_trade, allow_recycle, allow_marketplace_sell,
            allow_inventory_stack, item_type, interaction_type,
            interaction_modes_count, vending_ids, multiheight, customparams,
            effect_id_male, effect_id_female, clothing_on_walk, page_id, rare
        ) = row[:27]

        item_type        = str(item_type or "s").strip().lower()
        interaction_type = str(interaction_type or "default").strip().lower()

        product_type = PRODUCT_TYPE_MAP.get(item_type, 0)
        category     = get_furni_category(item_type, interaction_type)
        logic        = get_logic(interaction_type)
        extra_data   = str(customparams).strip() if customparams else None

        ib_id_int   = int(ib_id)
        sprite_int  = int(sprite_id) if sprite_id is not None else 0

        # Always populate lookup so catalog_products can resolve product_type
        lookup[ib_id_int] = {
            "sprite_id":    sprite_int,
            "item_name":    str(item_name or ""),
            "type":         item_type,
            "product_type": product_type,
            "category":     category,
            "allow_gift":   bool(int(allow_gift or 1)),
        }

        # Deduplicate by Turbo's unique index (sprite_id, type, category)
        key = (sprite_int, product_type, category)
        if key in seen_key:
            id_remap[ib_id_int] = seen_key[key]
            dupes += 1
            continue
        seen_key[key] = ib_id_int

        width_val  = int(width)  if width  is not None else 1
        length_val = int(length) if length is not None else 1
        sh_val     = float(stack_height) if stack_height is not None else 0.0

        cols = (
            f"({sql_str(ib_id_int)}, {sql_str(sprite_int)}, "
            f"{sql_str(str(item_name or ''))}, {sql_str(product_type)}, "
            f"{sql_str(category)}, {sql_str(logic)}, "
            f"{sql_str(int(interaction_modes_count) if interaction_modes_count is not None else 1)}, "
            f"{sql_str(width_val)}, {sql_str(length_val)}, {sql_str(sh_val)}, "
            f"{sql_bool(int(allow_stack or 1))}, "
            f"{sql_bool(int(allow_walk  or 0))}, "
            f"{sql_bool(int(allow_sit   or 0))}, "
            f"{sql_bool(int(allow_lay   or 0))}, "
            f"{sql_bool(int(allow_recycle or 0))}, "
            f"{sql_bool(int(allow_trade  or 1))}, "
            f"1, "
            f"{sql_bool(int(allow_marketplace_sell or 0))}, "
            f"0, "
            f"{sql_str(extra_data if extra_data else None)}, "
            f"NOW(), NOW(), NULL)"
        )
        lines.append(
            f"INSERT IGNORE INTO `furniture_definitions` "
            f"(`id`,`sprite_id`,`name`,`type`,`category`,`logic`,`total_states`,"
            f"`width`,`length`,`stack_height`,`can_stack`,`can_walk`,`can_sit`,`can_lay`,"
            f"`can_recycle`,`can_trade`,`can_group`,`can_sell`,`usage_policy`,"
            f"`extra_data`,`created_at`,`updated_at`,`deleted_at`) VALUES {cols};"
        )
        written += 1

    dst.write_text("\n".join(lines) + "\n", encoding="utf-8")
    print(f"  -> {dst.name}: {written} definitions written, {dupes} duplicates remapped")
    return lookup, id_remap


# ---------------------------------------------------------------------------
# 2. catalog_pages → catalog_pages (Turbo)
# ---------------------------------------------------------------------------
def convert_catalog_pages(src: Path, dst: Path) -> None:
    rows = iter_inserts(src)

    lines: list[str] = [
        "-- catalog_pages converted from Arcturus",
        "-- Generated by tools/catalog_converter/convert.py",
        "",
        "SET NAMES utf8mb4;",
        "SET FOREIGN_KEY_CHECKS = 0;",
        "",
    ]

    skipped  = 0
    dupes    = 0
    seen_ids: set[int] = set()
    for row in rows:
        if len(row) < 22:
            skipped += 1
            continue

        page_id_raw = row[0]
        page_id_int = int(page_id_raw)
        if page_id_int in seen_ids:
            dupes += 1
            continue
        seen_ids.add(page_id_int)

        (
            page_id, parent_id, caption_save, caption, page_layout,
            icon_color, icon_image, min_rank, order_num,
            visible, enabled, club_only, vip_only,
            page_headline, page_teaser, page_special,
            page_text1, page_text2, page_text_details, page_text_teaser,
            room_id, includes
        ) = row[:22]

        # parent_id: -1 in Arcturus means root (no parent)
        parent_val = "NULL" if (parent_id is None or int(parent_id) < 0) else sql_str(int(parent_id))

        # localization key: prefer caption_save if not empty, else derive from caption
        localization = str(caption_save or "").strip()
        if not localization:
            localization = str(caption or "").strip().lower().replace(" ", "_")[:50]

        name_val = str(caption or "").strip()[:50]

        # layout: map Arcturus → Turbo
        layout_str   = str(page_layout or "default_3x3").strip().lower()
        turbo_layout = LAYOUT_MAP.get(layout_str, "default_3x3")

        # icon (use icon_image; icon_color is dropped)
        icon_val = int(icon_image) if icon_image is not None else 0

        # visible: true if both visible=1 AND enabled=1
        is_visible = (str(visible or "0") == "1") and (str(enabled or "0") == "1")

        # image_data  [headline, teaser, special]
        image_data = json_array([page_headline, page_teaser, page_special])

        # text_data   [text1, text2, text_details, text_teaser]
        text_data = json_array([page_text1, page_text2, page_text_details, page_text_teaser])

        sort = int(order_num) if order_num is not None else 0

        lines.append(
            f"INSERT IGNORE INTO `catalog_pages` "
            f"(`id`,`parent_id`,`localization`,`name`,`icon`,`layout`,"
            f"`image_data`,`text_data`,`sort_order`,`visible`,"
            f"`created_at`,`updated_at`,`deleted_at`) VALUES "
            f"({sql_str(int(page_id))},{parent_val},{sql_str(localization[:50])},"
            f"{sql_str(name_val)},{sql_str(icon_val)},{sql_str(turbo_layout)},"
            f"{image_data},{text_data},{sql_str(sort)},{sql_bool(is_visible)},"
            f"NOW(),NOW(),NULL);"
        )

    dst.write_text("\n".join(lines) + "\n", encoding="utf-8")
    written = len(rows) - skipped - dupes
    print(f"  -> {dst.name}: {written} pages written, {dupes} duplicates skipped, {skipped} malformed skipped")


# ---------------------------------------------------------------------------
# 3. catalog_items → catalog_offers + catalog_products
# ---------------------------------------------------------------------------
def convert_catalog_items(src: Path, offers_dst: Path, products_dst: Path, lookup: dict[int, dict], id_remap: dict[int, int]) -> None:
    rows = iter_inserts(src)

    offer_lines: list[str] = [
        "-- catalog_offers converted from Arcturus catalog_items",
        "-- Generated by tools/catalog_converter/convert.py",
        "",
        "SET NAMES utf8mb4;",
        "SET FOREIGN_KEY_CHECKS = 0;",
        "",
    ]
    product_lines: list[str] = [
        "-- catalog_products converted from Arcturus catalog_items",
        "-- Generated by tools/catalog_converter/convert.py",
        "-- NOTE: currency_type_id references currency_types.id.",
        "--       Insert your currency_types rows first, then update",
        "--       catalog_offers.currency_type_id to match.",
        "",
        "SET NAMES utf8mb4;",
        "SET FOREIGN_KEY_CHECKS = 0;",
        "",
    ]

    product_id = 1
    skipped = 0
    warn_currency: set[int] = set()

    for row in rows:
        if len(row) < 18:
            skipped += 1
            continue

        (
            item_id, item_ids_raw, page_id, offer_id_arc,
            song_id, order_number, catalog_name,
            cost_credits, cost_points, points_type, amount,
            limited_sells, limited_stack, extradata, badge,
            have_offer, club_only, rate
        ) = row[:18]

        item_id_val   = int(item_id)
        page_id_val   = int(page_id)
        credits_val   = int(cost_credits) if cost_credits is not None else 0
        currency_val  = int(cost_points)  if cost_points  is not None else 0
        points_type_v = int(points_type)  if points_type  is not None else 0
        amount_val    = int(amount)        if amount        is not None else 1
        ltd_stack     = int(limited_stack) if limited_stack is not None else 0
        ltd_sells     = int(limited_sells) if limited_sells is not None else 0
        ltd_remaining = max(0, ltd_stack - ltd_sells)
        extra_param   = str(extradata or "").strip() or None
        is_visible    = str(have_offer or "1") == "1"
        club_level    = 1 if str(club_only or "0") == "1" else 0
        loc_id        = str(catalog_name or "").strip()

        # currency_type_id: NULL when no secondary currency (points = 0)
        if currency_val == 0 or points_type_v == 0:
            currency_type_id = "NULL"
        else:
            if points_type_v not in warn_currency:
                warn_currency.add(points_type_v)
                print(
                    f"  [INFO] points_type={points_type_v} found -> "
                    f"update catalog_offers.currency_type_id manually "
                    f"to match your currency_types row for activity_point_type={points_type_v}",
                    file=sys.stderr,
                )
            currency_type_id = sql_str(points_type_v)

        # can_gift: use allow_gift from first item_id in the set
        can_gift = True
        raw_ids_str = str(item_ids_raw or "").strip()
        item_id_list: list[int] = []
        for tid in raw_ids_str.split(","):
            tid = tid.strip()
            if tid.isdigit():
                item_id_list.append(int(tid))
        if item_id_list and item_id_list[0] in lookup:
            can_gift = lookup[item_id_list[0]]["allow_gift"]

        # --- catalog_offers row ---
        offer_lines.append(
            f"INSERT INTO `catalog_offers` "
            f"(`id`,`page_id`,`localization_id`,`cost_credits`,`cost_currency`,"
            f"`currency_type_id`,`can_gift`,`can_bundle`,`club_level`,"
            f"`discount_percent`,`visible`,`created_at`,`updated_at`,`deleted_at`) VALUES "
            f"({sql_str(item_id_val)},{sql_str(page_id_val)},{sql_str(loc_id)},"
            f"{sql_str(credits_val)},{sql_str(currency_val)},"
            f"{currency_type_id},{sql_bool(can_gift)},1,{sql_str(club_level)},"
            f"0,{sql_bool(is_visible)},NOW(),NOW(),NULL);"
        )

        # --- catalog_products rows ---

        # badge product (add before furni products if badge is present)
        badge_val = str(badge or "").strip() if badge is not None else ""
        if badge_val:
            product_lines.append(
                f"INSERT INTO `catalog_products` "
                f"(`id`,`offer_id`,`product_type`,`definition_id`,`extra_param`,"
                f"`quantity`,`unique_size`,`unique_remaining`,"
                f"`created_at`,`updated_at`,`deleted_at`) VALUES "
                f"({sql_str(product_id)},{sql_str(item_id_val)},3,NULL,"
                f"{sql_str(badge_val)},1,0,0,NOW(),NOW(),NULL);"
            )
            product_id += 1

        # song_id product (SoundTrack – product_type kept as Floor; flag for review)
        song_id_v = int(song_id) if song_id is not None else 0
        if song_id_v:
            product_lines.append(
                f"-- [MANUAL REVIEW] song_id={song_id_v} for offer {item_id_val} — "
                f"Turbo has no direct sound track product_type; "
                f"adjust product_type manually if needed."
            )

        # furni / effect / bot products from item_ids list
        for def_id in item_id_list:
            # Remap to canonical definition_id if this one was deduplicated
            canonical_id = id_remap.get(def_id, def_id)
            furni_info   = lookup.get(def_id)
            prod_type    = furni_info["product_type"] if furni_info else 0
            product_lines.append(
                f"INSERT IGNORE INTO `catalog_products` "
                f"(`id`,`offer_id`,`product_type`,`definition_id`,`extra_param`,"
                f"`quantity`,`unique_size`,`unique_remaining`,"
                f"`created_at`,`updated_at`,`deleted_at`) VALUES "
                f"({sql_str(product_id)},{sql_str(item_id_val)},{sql_str(prod_type)},"
                f"{sql_str(canonical_id)},{sql_str(extra_param)},"
                f"{sql_str(amount_val)},{sql_str(ltd_stack)},{sql_str(ltd_remaining)},"
                f"NOW(),NOW(),NULL);"
            )
            product_id += 1

    offers_dst.write_text("\n".join(offer_lines) + "\n", encoding="utf-8")
    products_dst.write_text("\n".join(product_lines) + "\n", encoding="utf-8")
    valid = len(rows) - skipped
    print(f"  -> {offers_dst.name}: {valid} offers ({skipped} skipped)")
    print(f"  -> {products_dst.name}: {product_id - 1} products")


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------
def main() -> None:
    items_base_sql    = INPUT_DIR / "items_base.sql"
    catalog_pages_sql = INPUT_DIR / "catalog_pages.sql"
    catalog_items_sql = INPUT_DIR / "catalog_items.sql"

    missing = [p for p in (items_base_sql, catalog_pages_sql, catalog_items_sql) if not p.exists()]
    if missing:
        print("Missing input files:", [str(p) for p in missing])
        sys.exit(1)

    print("Converting items_base ...")
    lookup, id_remap = convert_items_base(items_base_sql, OUTPUT_DIR / "furniture_definitions.sql")

    print("Converting catalog_pages ...")
    convert_catalog_pages(catalog_pages_sql, OUTPUT_DIR / "catalog_pages.sql")

    print("Converting catalog_items ...")
    convert_catalog_items(
        catalog_items_sql,
        OUTPUT_DIR / "catalog_offers.sql",
        OUTPUT_DIR / "catalog_products.sql",
        lookup,
        id_remap,
    )

    print("\nDone. Output files are in:", OUTPUT_DIR)
    print("Import order: furniture_definitions -> catalog_pages -> catalog_offers -> catalog_products")
    print()
    print("Notes:")
    print("  • furniture_definitions: public_name, multiheight, vending_ids, effects dropped")
    print("  • catalog_pages: min_rank, club_only, vip_only, room_id, includes dropped")
    print("  • catalog_offers: order_number dropped (no column in Turbo)")
    print("  • catalog_offers: song_id rows marked [MANUAL REVIEW] in catalog_products.sql")
    print("  • currency_type_id: set to the points_type value as a placeholder;")
    print("    update it after inserting your currency_types rows")


if __name__ == "__main__":
    main()