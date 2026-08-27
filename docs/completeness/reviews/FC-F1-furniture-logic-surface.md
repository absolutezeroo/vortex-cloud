# FC-F1 — the furniture logic surface

**Generated:** `docs/completeness/generated/FURNITURE-LOGIC.md`, on every `completeness --write`
**Denominator:** `Vortex.Database/Seeds/furni_logic_bindings.sql`

## The third surface

```
packets    578 obligations   404 implemented   one interaction each
wired      184 boxes         154 bound         one box each
furniture   73 logic names   3 947 definitions stranded
```

This one has the widest blast radius per gap. A missing packet costs one interaction. A logic name
nothing binds costs **every definition carrying it**, silently: the furni resolves to its family
default, places fine, sits there and does nothing. No error, no log a player can see — only "it
doesn't work".

## Where the denominator comes from, and why not the database

`furniture_definitions.logic` decides the behaviour, and it is populated by the committed
asset-derived binding pass, which was itself generated from the `.nitro` bundles (the assets are the
authority; the furnidata JSON does not carry a logic key at all). Reading the live database would
have meant a MySQL dependency inside a static-analysis project and a connection string this checkout
does not have — for a fact that is already committed.

The seed covers 42 059 definitions across 73 logic names. Definitions that already carried a
registered Vortex logic are **absent from it by design**, which is exactly right here: those were
never at risk.

## What it says

**38 112 / 42 059 answered (90.6%). 3 947 stranded.**

Worst first, because definitions impacted is the only thing separating an intentional fallback from
an accident:

| definitions | logic |
|---:|---|
| 2 862 | `furniture_purchasable_clothing` |
| 418 | `furniture_soundblock` |
| 135 | `furniture_credit` |
| 61 | `furniture_trophy` |
| 61 | `furniture_window` |
| 53 | `furniture_crafting_gizmo` |
| 53 | `furniture_pet_customization` |
| 47 | `furniture_change_state_when_step_on` |
| 45 | `furniture_multiheight` |
| 30 | `furniture_badge_display` |

Then a long tail down to two: `furniture_lovelock`, `furniture_pushable`,
`furniture_random_teleport`, `furniture_youtube`, `furniture_sound_machine`, `furniture_jukebox`,
`furniture_stickie`, `furniture_mannequin`, `furniture_gift`, and so on.

**`furniture_purchasable_clothing` alone is 2 862 definitions** — 72% of everything stranded, and
one binding. It is the single highest-leverage furniture gap in the hotel.

Note also `furniture_badge_display` (30) and `furniture_custom_stack_height` — FC-2 found the client
sends `3159` and `3315` for exactly those two, and both are missing on the packet side too. The two
surfaces agree about which features are absent, from opposite directions.

## What it found by accident

The seed writes names the assets carry verbatim, and some of those are not logic names at all:
`Cascata`, `Ondas`, `Gota`, `Àguas`, `Chama D'agua`, `Furniture`, `furniture_Static`,
`furniture_V_aianas` — asset metadata leaking into the field. Two definitions each, so the cost is
trivial; the point is that the field contains whatever the asset said, and the report shows it
rather than filtering it into a tidier picture.

`furniture_muItistate` — a capital I where an l belongs — is an asset typo on seven definitions, and
it is **deliberately registered** on both floor and wall so those seven still resolve. Someone had
already caught it. The report shows it as answered, which is correct and worth reading twice.

## Two limits, stated

**Name-level, not family-level.** The provider keys a logic by name *and* family (Floor/Wall/Any),
because the client calls both a gate and a picture `furniture_multistate`. A name registered for
floor only still leaves a wall definition carrying it on the default. This report does not see that
yet, so 90.6% is an upper bound.

**Registered is not correct.** Same limit as the other two surfaces: a binding means a class answers
to the name, not that it does the right thing. `verified complete` is the number that measures that,
and it is still 0.

## The next slices this ranks

1. `furniture_purchasable_clothing` — 2 862 definitions behind one logic.
2. `furniture_soundblock` — 418, and it pairs with the jukebox/sound-machine cluster the packet
   matrix already lists as eight stub obligations.
3. `furniture_badge_display` + `furniture_custom_stack_height` — small, and their packets are named
   and waiting from FC-2.
