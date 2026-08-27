# FC-W1 — the wired box surface

**Target client:** `WIN63-202607011411-782849652`
**Generated:** `docs/completeness/generated/WIRED-BOXES.md`, on every `completeness --write`

## Why a second denominator

The packet matrix scores the wired domain at **42/43**. That number is true and it is about
messages: the six `Update*` packets that carry a box's configuration, plus the wired-menu reads and
writes. Those messages are exactly as well implemented whether the box they configure exists here or
not.

So a player can drag a box out of the catalogue, open it, fill in its form, save it, receive
`WiredSaveSuccess` — and it never fires. Nothing in the packet report can see that, and nothing
would have.

The box surface says **154/184**. Both numbers describe the wired subsystem and they differ by
fourteen points, because they measure different things.

## The denominator

The client's own constant tables, one per family, under
`com/sulake/habbo/roomevents/wired_setup`:

| file | family | codes |
|---|---|---:|
| `triggerconfs/TriggerConfCodes.as` | trigger | 26 |
| `actiontypes/ActionTypeCodes.as` | action | 58 |
| `conditions/ConditionCodes.as` | condition | 45 |
| `selectors/SelectorCodes.as` | selector | 20 |
| `addons/AddonCodes.as` | addon | 26 |
| `variables/VariableCodes.as` | variable | 9 |

The join key is the integer code, which is what both sides route on. Numbering restarts inside each
family, so a code is only an identity together with its family.

Vortex's side is every `FurnitureWiredLogic` subclass declaring
`WiredCode => (int)Wired<Family>Type.MEMBER`, resolved through the enum files. 154 of them.

## Two things this found about itself

**The variable family read 0/9 on the first run.** Vortex's variable boxes route on
`WiredVariableBoxType`, not the similarly named `WiredVariableType` — the first names the box
families (Furni, User, Global, Context…), the second names what kind of value a variable holds. The
analyzer reported seven logics it could not place rather than silently scoring them missing, which
is how the mistake was visible in ten seconds instead of shipping as a fake gap.

**An enum member is not a box.** `WiredActionType` declares 47 members; 44 have a logic.
`SET_FURNI_STATE` (3), `TELEPORT` (8) and `GIVE_REWARD` (17) are named in the enum with nothing
behind them — which reads, to anyone grepping, exactly like an implemented box.

## The 30 gaps

**action — 14.** `SET_FURNI_STATE`(3), `TELEPORT`(8), `GIVE_REWARD`(17), `TELEPORT_TO_ROOM`(44),
49, 50, `PROGRESS_ACHIEVEMENT`(51), `OVERRIDE_HEIGHT`(53), 54, `PLACE_FURNI`(55), `REMOVE_FURNI`(56),
`MOVE_AS_GROUP`(57), `PROGRESS_REWARD_TRACK`(58), `RESET_REWARD_TRACK`(59).

**addon — 10.** `VARIABLE_CAPTURER`, `EXECUTE_IN_ORDER`, `CHEST_ITEM_TYPE_SCANNER`, `PROJECTILE`,
`JUMP_STRENGTH`, `VARIABLE_TEXT_CONVERTER`, `VARIABLE_LEVEL_UP`, `VARIABLE_TIME_UTIL`,
`GLOBAL_PLACEHOLDER`, `ACHIEVEMENT_ENABLER`.

**condition — 4.** `CAN_PERFORM_MOVE`(39), `USER_LEVEL`(44), `CHEST_HAS_ITEMS`(45),
`CHEST_HAS_ITEM_TYPES`(46).

**variable — 2.** `ECHO_VARIABLE`(7), and code 8 whose name did not survive.

**trigger and selector are complete.** 26/26 and 20/20.

### Clusters worth reading as one job

- `PROJECTILE` + `JUMP_STRENGTH` are the ball physics the game framework already reserved a seam for.
- `CHEST_HAS_ITEMS` + `CHEST_HAS_ITEM_TYPES` + `CHEST_ITEM_TYPE_SCANNER` are one chest-inspection
  cluster, and wired chests are already built.
- `PLACE_FURNI` + `REMOVE_FURNI` + `MOVE_AS_GROUP` are furniture manipulation the room grain can
  already do for other boxes.
- `PROGRESS_REWARD_TRACK` + `RESET_REWARD_TRACK` need the reward track, which FC-2 found is also
  missing on the packet side (`1376`, `1789`).

## What this deliberately does not do

It does not audit the V4 engine, and it says nothing about whether an implemented box is *correct* —
only that a logic is bound to the code. That is the same limit the packet matrix has, and the same
answer applies: `verified complete` is the number that measures correctness, and it is still 0.

An obfuscated client constant is recorded as nameless rather than named after its mangling. Four
boxes are in that state; their codes are what matters and they are what a slice would implement
against.
