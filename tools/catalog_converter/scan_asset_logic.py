#!/usr/bin/env python3
"""
Asset logic scanner
===================
Reads the room-object logic each furni actually binds to out of the shipped `.nitro` assets, and
writes it to `data/furni_logic_overrides.json` for convert.py to apply.

Why this exists
---------------
The Arcturus dump the converter reads has no logic column of its own: it carries an
`interaction_type`, which convert.py used to copy verbatim into `furniture_definitions.logic`. For
most furni that happens to line up with a Vortex logic name, but for guild furniture it does not --
the dump says `guild_furni` / `guild_gate` / `none`, none of which is registered, so every one of
those furni fell back to `default_floor` and lost both its behaviour and (because the format follows
the logic) its colours.

The assets are the authority: every `<classname>.nitro` embeds a JSON part whose `logicType` names
the client logic that renders it. This scanner reads that field, so the mapping is derived rather
than guessed, and re-derives itself when the furni pack is updated.

Only the logic families that need a non-default stuff-data format are emitted. Everything else keeps
convert.py's existing behaviour -- the point is to fix a known break, not to repoint the whole
catalogue at client logic names, which would break the wired furni whose Vortex logic names
deliberately match Arcturus (`wf_act_*`, `wf_cnd_*`, ...).

Usage
-----
    python scan_asset_logic.py <furni-pack-dir>

    e.g. python scan_asset_logic.py C:/Laragon/www/vortex-assets/dcr/hof_furni
"""

import gzip
import json
import struct
import sys
import zlib
from pathlib import Path

BASE_DIR = Path(__file__).parent
DATA_DIR = BASE_DIR / "data"
OUTPUT = DATA_DIR / "furni_logic_overrides.json"

# Client logic name -> (Vortex logic name, stuff_data_type).
#
# stuff_data_type 2 is StuffDataType.StringKey: the client reads these as a string array, with the
# guild id, badge code and both recolours at indices 1..4.
#
# `furniture_guild_customized` maps to itself. The forum terminal keeps the client's name too. The
# gate is the one deliberate divergence: the asset gives `gld_gate` the same
# `furniture_guild_customized` as a guild carpet, because the Flash client derives blocking from the
# visualization -- Vortex resolves walkability server-side and so needs a gate logic. That single
# remap lives in GATE_OVERRIDE rather than here, since it is keyed by classname, not by logic.
LOGIC_MAP = {
    "furniture_guild_customized": ("furniture_guild_customized", 2),
    "furniture_group_forum_terminal": ("furniture_group_forum_terminal", 2),
}

GATE_OVERRIDE = {"gld_gate": ("furniture_guild_gate", 2)}


def read_logic_type(path: Path) -> str | None:
    """
    Pulls `logicType` out of a .nitro container.

    Layout is [u16 part-count] then, per part, [u16 name-length][name][u32 data-length][data].
    Part payloads are compressed, and the pack mixes two formats -- gzip and raw zlib -- so both are
    tried. Reading only gzip silently skips roughly half the pack.
    """
    data = path.read_bytes()
    offset = 0

    (count,) = struct.unpack_from(">H", data, offset)
    offset += 2

    for _ in range(count):
        (name_len,) = struct.unpack_from(">H", data, offset)
        offset += 2
        name = data[offset : offset + name_len].decode("utf-8", "ignore")
        offset += name_len

        (blob_len,) = struct.unpack_from(">I", data, offset)
        offset += 4
        blob = data[offset : offset + blob_len]
        offset += blob_len

        if not name.endswith(".json"):
            continue

        try:
            raw = gzip.decompress(blob)
        except Exception:
            raw = zlib.decompress(blob)

        return json.loads(raw).get("logicType")

    return None


def main() -> None:
    if len(sys.argv) < 2:
        print(__doc__)
        sys.exit(1)

    pack = Path(sys.argv[1])
    if not pack.is_dir():
        print(f"Not a directory: {pack}")
        sys.exit(1)

    overrides: dict[str, dict] = {}
    scanned = 0
    unreadable: list[str] = []

    for asset in sorted(pack.glob("*.nitro")):
        classname = asset.stem
        try:
            logic_type = read_logic_type(asset)
        except Exception:
            unreadable.append(classname)
            continue

        scanned += 1

        mapped = GATE_OVERRIDE.get(classname) or LOGIC_MAP.get(logic_type or "")
        if mapped is None:
            continue

        logic, stuff_data_type = mapped
        overrides[classname] = {"logic": logic, "stuff_data_type": stuff_data_type}

    DATA_DIR.mkdir(exist_ok=True)
    OUTPUT.write_text(
        json.dumps(overrides, indent=2, sort_keys=True) + "\n", encoding="utf-8"
    )

    print(f"Scanned {scanned} assets ({len(unreadable)} unreadable)")
    print(f"Wrote {len(overrides)} overrides to {OUTPUT}")

    if unreadable:
        # Loud on purpose: a decoder that silently skips assets under-reports the mapping, and the
        # missing furni then look correct in the diff while staying broken in-game.
        print(f"  [WARN] unreadable assets: {unreadable[:10]}{' ...' if len(unreadable) > 10 else ''}")


if __name__ == "__main__":
    main()
