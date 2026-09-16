# Editing gamedata from the dashboard — design

**Date**: 2026-08-28
**Status**: proposed, not implemented

Make the four files the client downloads editable from the dashboard:
`external_variables.json`, `external_flash_texts.json`, `furnidata_json.json`, `productdata_json.json`.
And do it **multilingual from the start**, because the client can do it and nothing uses it.

`figuredata.xml` and `figuremap.xml` are **out of scope** — an explicit decision, not an oversight.

---

## 1. What exists today

The files live on the asset host, `C:/Laragon/www/vortex-assets/gamedata/`. That root is
already configured on the dashboard side (`DashboardAssetUrls.LocalRoot`) and `gamedata` already appears in
`HotelAssetRoots`.

| File | Size | Shape | Entries |
| --- | --- | --- | --- |
| `external_variables.json` | 42 KB | flat map | 709 |
| `external_flash_texts.json` | 1,071 KB | flat map | 12,529 |
| `productdata_json.json` | 3 MB | `productdata.product[]` | — |
| `furnidata_json.json` | **38 MB** | `roomitemtypes` / `wallitemtypes` | **55,836** |

`hashes.php` publishes `{name, url, hash: md5_file(...)}` and `.htaccess` rewrites `^<name>/.+$` to the
file. The client requests `<url>/<hash>`. **Free consequence: any write changes the md5,
hence invalidates the client cache.** No cache-busting work to do, but `hashes.php` must
know every file served — including the new per-language files (§4).

The `gamedata/en/` and `gamedata/fr/` folders exist and are **empty**.

---

## 2. Multilingual — what the target client can do

Authority: `vortex-modern-client`. The 2016 source is readable, the WIN63-2026 target client is
verified by symbol presence.

`HabboLocalizationManager.configureLocalizationLocations()` loops `k = 1, 2, 3…` for as long as
`localization.<k>` exists, and reads for each:

| Key | Role |
| --- | --- |
| `localization.<k>` | the language **id** |
| `localization.<k>.code` | the code (`fr`) |
| `localization.<k>.name` | the display name (`Français`) |
| `localization.<k>.url` | **the URL of that language's texts file** |

then `registerLocalizationDefinition(id, name, url, code)`.
`requestLocalizationInit()` then loads `external.texts.txt`: that is the **base**, the default language.

Switching is done via **`activateLocalizationDefinition(id)`**, which makes the definition active
*and reloads its URL*. Its only caller is `ChatInputWidgetHandler`, on the chat command
**`:lang <id>`**.

> `:lang` takes the **id**, not the code. So we set `localization.<k>` to the code itself, so the
> player types `:lang fr`. Without that the feature exists and stays undiscoverable.

### What the target lost

| Mechanism | 2016 | WIN63-2026 target |
| --- | --- | --- |
| `localization.<k>` registry | ✅ | ✅ `configureLocalizationLocations`, `registerLocalizationDefinition`, `localization.1` present |
| `external.texts.txt` | ✅ | ✅ |
| `external.override.texts.txt` (2nd layer) | ✅ | ❌ **absent** |
| `language_selection.enabled` | ✅ | ❌ **absent — dead key in the current dump** |

No override layer on the target: **a text change is written into its own language's file**,
there is no override file alongside the dump.

### The boundary, written in black and white

`furnidata.load.url` and `productdata.load.url` are plain properties, loaded **once,
as is**. There is no equivalent registry and no `%lang%` substitution in the client
(verified: no occurrences). Furni names live in furnidata (only 4 `furni_*_name|desc`
keys in the texts, so that is not the route).

**The client offers no way to serve a per-language furnidata.** Furnidata and productdata are
therefore editable in **a single language**. Working around that would mean serving different content at the
same URL depending on the player — an infrastructure decision on the asset host, outside this
design. That limit is a property of the client, not a shortcut.

---

## 3. Write model

The dump is **frozen** (operator's decision: no re-import of an official dump). The file is
therefore the truth, edited in place.

Every write, without exception:

1. timestamped backup copy;
2. write to a temporary file;
3. **re-read and parse the temporary file**;
4. atomic replacement of the real file.

Step 3 is the heart of it: a file the client downloads must never be able to stay broken. If
the parse fails, the real file has not moved and the operation is refused.

Backups go to `gamedata_backups/`, a **sibling** of `gamedata/` and therefore outside the three
roots served by `HotelAssets` — otherwise the backups would be downloadable by anyone.

### Concurrency

Every write carries the file's expected `mtime`. If it moved, refuse. Several people
touch the hotel, and a lost write on a file of 55,836 entries is invisible.

### Cache and cost

`GamedataDocumentStore` parses on first request and keeps it in memory, invalidated on write.
Search and pagination **server-side**: the page never receives the file.

> `ponytail:` full rewrite of the 38 MB on every furni save (~1 s of disk).
> That is the price of "the file is the truth". Move to an incremental write only if
> the wait becomes annoying.

---

## 4. The languages

**One single language list for the whole hotel.** `web_languages` already exists (`Code`, `Name`,
`IsDefault`, `Enabled`), created for the news. We reuse it as the **list**. No second table:
two language lists diverge, always.

But `web_languages.Enabled` means "publishable on the site" and nothing else. A language can be
open on the site without its 12,529 client texts being translated, and the reverse is true too.
The two states are therefore **distinct**: the site keeps `Enabled`, the client is enabled separately
from this tab, and the presence of the `localization.<k>` block in `external_variables.json` **is**
that state — there is no second flag in the database to keep in sync with the file.

Enabling a language **for the client** produces, in one operation:

1. the `localization.<k>` / `.code` / `.name` / `.url` block in `external_variables.json` —
   **generated, never hand-typed**, and renumbered from 1 to N on every change (the client stops
   at the first gap);
2. the `gamedata/<code>/external_flash_texts.json` file, initialized from the default language;
3. the corresponding entry in `hashes.php`.

Disabling a language removes its block and its hash entry; **the texts file is kept**
(translation work is not lost on a click).

The default language stays served by `external.texts.txt`, that is, the root
`external_flash_texts.json`.

---

## 5. Entry identity

| File | Key |
| --- | --- |
| `external_variables.json` | the key |
| `external_flash_texts.json` | the key + the language code |
| `productdata_json.json` | the product code |
| `furnidata_json.json` | **`(kind, index)`** |

For furnidata, neither `id` nor `classname` identifies an entry:

- 55,836 entries for **55,254 distinct ids** and **51,425 distinct classnames**;
- 577 ids are shared between `roomitemtypes` and `wallitemtypes` — two namespaces, legitimate;
- **5 ids are duplicated inside `roomitemtypes` itself** (e.g. `2170666`): two entries,
  same id, same list. A flaw in the dump; which one the client keeps is unknown.

Hence: the key is the position in the array, and **deleting a furni entry is not
supported** (the indices would shift). Editing and appending at the end of the list only.

---

## 6. furnidata ↔ database coherence

`furniture_definitions` is fed *from* furnidata, never the reverse. Both describe the
same furniture and share 7 fields:

| furnidata | `furniture_definitions` |
| --- | --- |
| `id` | `SpriteId` |
| `classname` | `Name` |
| `xdim` / `ydim` | `Width` / `Length` |
| `cansiton` / `canstandon` / `canlayon` | `CanSit` / `CanWalk` / `CanLay` |

Changing `xdim` without changing `Width` makes the client draw a 2×1 piece of furniture for which the server reserves
1×1. Neither the build, nor the tests, nor the screen flags it.

A **Coherence** tab lists those disagreements and offers "align the database" per row.

Join: `id` + `kind` → `SpriteId` + `ProductType` (`roomitemtypes` → floor, `wallitemtypes` → wall).
The table's unique index is `(SpriteId, ProductType, FurniCategory)`, so `(SpriteId, ProductType)`
can still point at several rows. **An ambiguous join is listed as ambiguous, never
settled at random** — and the 5 duplicates from §5 show up there too.

`Name` is non-unique by design (3,533 duplicates): never join on it, use
`FurnitureDefinitionLookup` for any resolution by classname.

---

## 7. HTTP surface

Reads, under `/api/v1/gamedata`:

| Route | Returns |
| --- | --- |
| `GET /files` | the 4 files: size, entry count, `mtime` |
| `GET /entries?file=&lang=&search=&page=` | paginated entries, server-side search |
| `GET /languages` | the languages, their `localization.<k>` block and their file's state |
| `GET /coherence?page=` | the disagreements from §6 |

Writes, under `/api/v1/operations/gamedata/…`, via `DashboardOperationsService` (so audited, and
captured by the before/after interceptor):
`entry/save`, `entry/delete` (vars and texts only), `furni/save`, `language/enable`,
`language/disable`, `coherence/align`.

`file` is a **closed enumeration of 4 values**, never a path. No path concatenation
coming off the network.

---

## 8. UI

One page, five tabs: **Variables · Texts · Furnidata · Products · Coherence**.

Each tab: a search box, a paginated table, an edit drawer. Reuses `createResource`,
`createWriteOps`, `Pagination`, `Drawer`, `Tabs`, `EmptyState` — all existing.

The **Texts** tab is the only multilingual one: one key per row, **one column per enabled language**,
missing translations flagged. It is that view that makes the translation work feasible;
a language picker would force comparing from memory.

Capability `gamedata.manage`, declared in the **four** files of the `AGENTS.md` checklist.

---

## 9. Known traps

- **Dashboard DI**: `GamedataDocumentStore` must be added to
  `DashboardWebHost.ForwardedServiceTypes`. An unforwarded service is read as a request body
  and kills the **entire** dashboard at startup.
- **Backups outside the served roots** (§3), otherwise they are public.
- **Renumbering `localization.<k>`**: the client stops at the first missing index. A gap
  makes every following language invisible.
- **`hashes.php` is generated** from the language list; forgetting it means the client never
  sees that a file changed.
- `.htaccess` must rewrite the per-language paths (`^<code>/external_flash_texts/.+$`).

---

## 10. Tests

- the write path: backup created, temporary parsed, atomic replacement — **and the refusal
  leaving the real file intact when the produced content does not parse**;
- the refusal on a stale `mtime`;
- generation of the `localization.<k>` block: contiguity from 1 to N after enabling *and* disabling;
- the coherence join, including one ambiguous case and one duplicate case;
- the refusal of a `file` outside the enumeration.

---

## 11. Slicing

| Slice | Contents |
| --- | --- |
| 1 | `GamedataDocumentStore` + safe write + language registry (`localization.<k>`, per-language files, `hashes.php`) + Variables tab |
| 2 | Multilingual Texts tab |
| 3 | Furnidata + Products + Coherence tab |

Slice 1 carries all the write machinery and the multilingual support, that is, the two things
everything else depends on.

---

## 12. Left open

- **Furnidata and productdata stay monolingual** (§2). The client allows nothing else.
- Re-importing a more recent official dump is not handled: the operator stated the dump
  is frozen. If that changes, the edits made here will be overwritten and an override layer will be
  needed — which the target no longer provides for texts.
