#!/usr/bin/env python3
"""
Asset logic scanner
===================
Writes the two reference files convert.py needs to bind imported furniture to real room-object
logic:

    data/asset_logic.json    classname -> the logicType its .nitro declares
    data/vortex_logics.json  registered Vortex logic name -> the families it serves (floor / wall)

Why this exists
---------------
The Arcturus dump has no logic column: it carries an `interaction_type`, which convert.py used to
copy verbatim into `furniture_definitions.logic`. Almost none of those strings is a registered Vortex
logic -- 53 782 of 55 279 definitions resolved to nothing and fell through to the family default, so
vending machines, beds, teleports and pressure plates silently did nothing.

The assets are the authority: every `<classname>.nitro` embeds a JSON part whose `logicType` names
the logic the client binds to.

The rule convert.py applies
---------------------------
Keep the dump's interaction_type when it is already a registered Vortex logic; otherwise take the
asset's logicType. Never the other way round. The client's names are not a superset of ours -- it
calls a gate `furniture_multistate`, because Flash derives blocking from the visualization while
Vortex resolves walkability server-side. Preferring the asset unconditionally overwrites 744 working
definitions, 375 of them gates that would stop blocking, plus the wired furni whose Vortex logic
names deliberately mirror Arcturus.

Usage
-----
    python scan_asset_logic.py <furni-pack-dir> [--repo <vortex-repo-root>]

    e.g. python scan_asset_logic.py C:/Laragon/www/vortex-assets/dcr/hof_furni
"""

import gzip
import json
import re
import struct
import sys
import zlib
from pathlib import Path

BASE_DIR = Path(__file__).parent
DATA_DIR = BASE_DIR / "data"
DEFAULT_REPO = BASE_DIR.parent.parent

ASSET_LOGIC_OUT = DATA_DIR / "asset_logic.json"
VORTEX_LOGICS_OUT = DATA_DIR / "vortex_logics.json"

LOGIC_ATTRIBUTE = re.compile(r'RoomObjectLogic\("([^"]+)"\)')

# gld_gate is the one deliberate divergence from the assets, which give it the same
# `furniture_guild_customized` as a guild carpet: the Flash client derives blocking from the
# visualization, Vortex resolves walkability server-side and so needs a gate logic. Folding it back
# into the shared logic would force gate rules onto every recoloured guild furni.
CLASSNAME_OVERRIDES = {"gld_gate": "furniture_guild_gate"}


def read_logic_type(path: Path) -> str | None:
    """
    Pulls `logicType` out of a .nitro container.

    Layout is [u16 part-count] then, per part, [u16 name-length][name][u32 data-length][data]. Part
    payloads are compressed, and the pack mixes two formats -- gzip and raw zlib -- so both are
    tried. Reading only gzip silently skips roughly half the pack, which under-reports the mapping
    while looking like a clean run.
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


def scan_vortex_logics(repo: Path) -> dict[str, list[str]]:
    """
    Collects every registered logic name and the families it serves.

    A name can serve both: `furniture_multistate` and `furniture_basic` each cover thousands of floor
    definitions and several hundred wall ones, and the provider keys its registry by (name, family)
    for exactly that reason. Family is read from the source path, which is how the logic classes are
    already organised.
    """
    logics: dict[str, set[str]] = {}

    for path in repo.rglob("*.cs"):
        parts = path.parts
        if "obj" in parts or "bin" in parts:
            continue

        try:
            source = path.read_text(encoding="utf-8", errors="ignore")
        except OSError:
            continue

        keys = LOGIC_ATTRIBUTE.findall(source)
        if not keys:
            continue

        directory = path.parent.as_posix()
        family = (
            "wall"
            if "/Wall" in directory
            else ("floor" if "/Floor" in directory else "any")
        )

        for key in keys:
            logics.setdefault(key, set()).add(family)

    return {k: sorted(v) for k, v in sorted(logics.items())}


def main() -> None:
    if len(sys.argv) < 2:
        print(__doc__)
        sys.exit(1)

    pack = Path(sys.argv[1])
    if not pack.is_dir():
        print(f"Not a directory: {pack}")
        sys.exit(1)

    repo = DEFAULT_REPO
    if "--repo" in sys.argv:
        repo = Path(sys.argv[sys.argv.index("--repo") + 1])

    asset_logic: dict[str, str] = {}
    unreadable: list[str] = []

    for asset in sorted(pack.glob("*.nitro")):
        classname = asset.stem
        try:
            logic_type = read_logic_type(asset)
        except Exception:
            unreadable.append(classname)
            continue

        logic_type = CLASSNAME_OVERRIDES.get(classname, logic_type)
        if logic_type:
            asset_logic[classname] = logic_type

    vortex_logics = scan_vortex_logics(repo)

    DATA_DIR.mkdir(exist_ok=True)
    ASSET_LOGIC_OUT.write_text(
        json.dumps(asset_logic, indent=0, sort_keys=True) + "\n", encoding="utf-8"
    )
    VORTEX_LOGICS_OUT.write_text(
        json.dumps(vortex_logics, indent=1, sort_keys=True) + "\n", encoding="utf-8"
    )

    print(f"Wrote {len(asset_logic)} asset logic bindings to {ASSET_LOGIC_OUT.name}")
    print(f"Wrote {len(vortex_logics)} Vortex logic names to {VORTEX_LOGICS_OUT.name}")

    if unreadable:
        # Loud on purpose: a decoder that silently skips assets under-reports the mapping, and the
        # furni it missed then look correct in the diff while staying inert in-game.
        print(
            f"  [WARN] {len(unreadable)} unreadable assets: {unreadable[:8]}"
            f"{' ...' if len(unreadable) > 8 else ''}"
        )


if __name__ == "__main__":
    main()
