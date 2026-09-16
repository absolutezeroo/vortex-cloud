# Silent gaps audit — wired and declaration seams

- **Audited revision**: `04550328f8d696358d477cc835c7d86dffb2b8bc` (`main`) + `d88948d` (beta report), branch `claude/vortex-cloud-beta-audit-apl5gn`.
- **Date**: 2026-09-15.
- **Question asked**: "there are quite a few problems around wired and everything that keep coming back, this isn't normal, I feel like the more we add the more we lose, we forget really dumb things."
- **Reference client**: `absolutezeroo/vortex-modern-client` at `cfeac29`, read as a source (TypeScript port; the AS3 `sources/` dump is not committed).
- **Nature**: read-only audit of the code, plus two mechanical checks shipped with the report (`scripts/hooks/check-inert-declarations.mjs`, `scripts/hooks/check-wired-param-counts.mjs`). No production code modified.

---

## 1. The one-page answer

The impression is right, and it has a single precise cause.

It is not carelessness, nor a "fragile" wired system. It is **one single bug class**, which keeps coming back because nothing watches it:

> **A declaration on one side of a seam, an implementation on the other, no compiler in between, and silence when they disagree.**

The box is placed, configured, saved, the build is green, the tests pass — and nothing happens. No exception, no error log, no refusal packet. The only way to notice is for a player, or you, to try the feature by hand.

This is not a hunch. Across the 51 commits of visible history, **19 fix a case of this class**; across the 22 `wired:` commits, **12**. The titles say it themselves:

| Commit | What was declared | What was missing opposite |
|---|---|---|
| `482acb1` | `CanWriteValue` on `@type` and `@position.x` | the write: the client offered the rows, selecting them did nothing |
| `0455032` | eight level readings | the flag: visible, never selectable |
| `c38f813` | the same readings | `CanReadCreationTime` / `CanReadLastUpdateTime` |
| `2ccb45b` | 21 parameter rules | the client sends 22: **all** saving was refused |
| `7c2ad33` | fixed rules | the client sends a variable count |
| `0aed855` | a variable box | `GetMaxVariableIds()` was 0: only one in three arrived |
| `5ae3929` | the reference variable dialog | context section 4 was never emitted: dialog greyed out |
| `4163006` | the echo variable | read fine, swallowed every write |
| `f17e75e` | the context variables tab | the readings: it could only answer zero |

Nine bugs, one shape. And **the same shape exists everywhere else in the repository**, not only in wired: a handler with no parser, a composer with no serializer, a settings key absent from the catalog, a furni logic not bound in the database.

And the knowledge was not missing. The `add-a-wired-box.md` walkthrough already warns, in bold, that the number of parameter rules must match what the client sends "*or the config is silently refused*". That text existed on 12 September; the two bugs of that type shipped on the 13th and the 14th. **The problem is therefore not what you know, it is that nothing checks it for you** (§7).

The repository has already understood the problem **case by case**: seven mechanical checks exist (`scripts/hooks/check-*.mjs`), and each was written after an incident. What is missing is the same reflex applied to the seams that do not have one yet — and a net that works (see §7: three of the existing checks are broken, so the barrier has been bypassed for eight commits).

**What I ship with this report**: two mechanical checks, both verified by the only proof that counts — rerunning them on the tree before a real bug's fix and checking that they find it.

- `check-inert-declarations.mjs` covers six seams at once and runs in 0.9 s. On the commit preceding `482acb1`, it surfaces exactly the two readings that commit fixes (§6).
- `check-wired-param-counts.mjs` closes the seventh, the most expensive one, by confronting each box with the client you supplied. On the commit preceding `2ccb45b`, it surfaces exactly that commit's two boxes, with the right delta, and nothing else (§5.4). **It also finds five boxes broken today, at HEAD**: `wf_trg_user_performs_action`, `wf_act_freeze_habbo`, `wf_slc_users_with_var`, `wf_slc_remote` and `wf_xtra_mov_physics` each refuse their entire configuration, silently, on every save (§5.3).

---

## 2. The seven seams, and who watches them

A "seam" is a place where two halves of the same feature are written separately and must match exactly.

| # | Seam | Reference authority | Effect of a disagreement | Watched before this audit |
|---|---|---|---|---|
| 1 | `GetIntParamRules()` rule count ↔ indices read at runtime | the code itself | the parameter always equals its default | no |
| 2 | `WiredVariableFlags` flags ↔ implemented methods | the code itself | the row is offered to the player and does nothing | test on the **only** User band |
| 3 | Inbound contract ↔ mapped parser ↔ handler | the code itself | the handler can never run | no |
| 4 | Composer built ↔ mapped serializer | the code itself | the packet disappears at the encoder | no |
| 5 | Key read from `IServerConfigGrain` ↔ `ConfigKeyCatalog` | the code itself | the setting exists, no operator sees it | no |
| 6 | Wired box declared ↔ behaviour that reads its configuration | the code itself | the box configures and does nothing | no |
| **7** | **Parameter rules ↔ what the client actually sends** | **the client's TS port, committed** | **all saving is silently refused** | **no — now `check-wired-param-counts.mjs`** |
| 8 | `[RoomObjectLogic]` key ↔ admin dropdown | the code | the logic exists, nobody can pick it | `check-logic-groups.mjs` |
| 9 | Mapped header id ↔ the client's registry | the client | the id registers and can never arrive | `check-header-registry.mjs` (degraded) |
| 10 | Dashboard capability ↔ 4 files | the code | the page is invisible to every operator | `check-dashboard-capabilities.mjs` |
| 11 | `furniture_definitions.logic` ↔ logic class | **the database** | the furni does not have the expected behaviour | metric only, and blind to the wired case |

The first six are covered by `check-inert-declarations.mjs`, the seventh by `check-wired-param-counts.mjs`. Number 11 still needs a decision (§4.4), and number 9 stays degraded when it no longer has any reason to be (§9).

---

## 3. The state of wired today: what is clean

This has to be said plainly, because the impression that "everything is degrading" does not match what the code shows: **wired is in good shape on three of the four internal seams.**

| Check | Result at HEAD |
|---|---|
| 173 concrete boxes: a parameter index read outside the declared rules | **0** |
| 45 concrete variable readings: a flag with no implementation (or the reverse) | **0** |
| 23 triggers: a room event nobody publishes | **0** |
| 394 composers built: one without a mapped serializer | **0** |
| Boxes with a commented-out implementation block | **1** |

The recent fixes really did close what they aimed at. The problem is not that wired is degrading: it is that **no way existed to know** that a regression of this shape had reappeared, short of hitting it again.

---

## 4. What remains open

### 4.1 Two inert wired boxes (confirmed by the code)

**`wf_slc_furni_with_var` — "select the furni carrying this variable"**
`Vortex.Rooms/Object/Logic/Furniture/Floor/Wired/Selectors/WiredSelectorItemsWithVariable.cs`

The box is complete on the configuration side: five parameter rules, a furni source, a player source, and the `AllVariablesInRoom` context that feeds the client's variable picker. It places, configures and saves.

`SelectAsync` (line 38 onwards) is a **commented-out 85-line block** and returns an empty selection. The commented code does not compile — it contains `if(_wiredData.Vari)` and `_ctx.Furni.GetVariableById()` with no argument: someone started the implementation, commented it out so the build would pass, and the box shipped like that.

Second defect on the same box: it does not override `GetMaxVariableIds()`, whose default is `0` (`FurnitureWiredLogic.cs:557`). `GetValidVariableIds` therefore caps at zero: even with the body uncommented, **no variable id would survive the save**. That is exactly the bug fixed by `0aed855` on another box, still present here.

*Player effect*: any chain using this selector selects zero furni, with no message.
*Fix*: implement `SelectAsync` and declare `GetMaxVariableIds() => 1`, or remove the box from the registry until it is written, documenting the inertness instead of hiding it — as `WiredTriggerHabboPerformsAction` does. The *practice* is the right one; in that particular case, the written justification is false and §5.3 corrects it.

**`wf_xtra_mov_carry_users` — the "carry users" add-on**
`Vortex.Rooms/Object/Logic/Furniture/Floor/Wired/Addons/WiredAddonCarryUsers.cs`

`FillInternalDataAsync` carefully reads the parameter into `_carryUserType`. `MutatePolicyAsync` returns `true` without ever reading it, and **`_carryUserType` is read nowhere else in the repository** (verified by searching the whole tree). The add-on is configurable and has no effect: avatars standing on a moving furni are not carried.

### 4.2 Six handlers that can never run

The inbound chain has three links: a message contract, a parser mapped to a header id, a handler. Six handlers have their contract and their parser **written**, and no `MapParser` line:

| Message | Verdict |
|---|---|
| `SetRelationshipStatusMessage` | **real gap.** The id exists (`Headers.cs:636`, value 1773), the parser exists, the handler exists; only the line in `UsersMap.cs` is missing — only the *Get* is mapped there (`UsersMap.cs:84`). A player can read their friends' relationship statuses and **never set one**. |
| `ChargeFireworkMessage` | **real gap.** Handler and parser written, no header id, no mapping. Charging a firework never reaches the server. |
| `GetTargetedOfferMessage` | dead weight. `GetNextTargetedOffer` (id 848) is mapped and does the same job; the triplet is a duplicate. |
| `GiveStarGemToUserMessage` | acknowledged dead weight: `Headers.cs:282` carries `= -1 // REMOVED in 2026`. |
| `class_165Message`, `class_200Message` | unidentified client classes, never mapped. To be named from the client or deleted. |

Two genuinely lost features, four pieces of dead code. The detector finds them in 0.9 s; nothing had found them until now.

### 4.3 Forty-eight settings nobody can set

`ConfigKeyCatalog` is what the dashboard's configuration editor displays. Forty-eight keys are read from `IServerConfigGrain` and **absent from the catalog**:

| Family | Keys | File |
|---|---|---|
| Pets | 22 | `Vortex.Rooms/Configuration/PetTuning.cs` |
| Fishing | 16 | `Vortex.Fishing/FishingConfig.cs` |
| Moderation | 4 | `Vortex.PacketHandlers/Configuration/ModerationConfig.cs` |
| Quests | 3 | `Vortex.Progression/Grains/PlayerQuestGrain.cs` |
| Collectibles, clothing, achievements | 3 | various |

The irony is in the code's own comments. `PetTuning.cs:8`: "*so they can be retuned from the dashboard without a restart*". `FishingConfig.cs:11-19`: "*exactly what `IServerConfigGrain` exists for*". The intent is written, the half that delivers it is missing. The room games (Freeze, Banzai, football), on the other hand, have their keys in the catalog — because a walkthrough requires it (`docs/walkthroughs/add-a-room-game.md`, step 3). The difference between the two families is not the care taken: it is the existence of a checklist.

*Fix*: one descriptor per key in `ConfigKeyCatalog.All`. An hour of work, and the shipped check stops the next one escaping.

### 4.4 The furni → logic binding, which lives in the database and which nothing checks

`scripts/sql/wired_logic_binding_fix.sql` says it in its own words:

> "Sixty do not — they say `furniture_multistate`, which is the generic "it has states" logic — so those boxes attach no wired behaviour and do nothing when a trigger reaches them. **Nothing reports it: an unresolved logic falls back silently.**"

And the `furni_logic_bindings.sql` seed tells the same story one step earlier: "*53,782 of 55,279 definitions resolved to nothing and fell through to the family default. Silently.*"

The `Vortex.furniture.logic.fallback` metric (`VortexMetrics.cs:126`, emitted by `RoomObjectLogicProvider.cs:143`) covers the "logic not found" case. It is **blind to the wired case**: a `wf_act_*` box whose `logic` column says `furniture_multistate` resolves perfectly — to a logic that is not a wired logic. No fallback, no counter, no log.

*Proposed fix, five lines*: when hydrating a room object, if the definition name matches `^wf_(act|cnd|trg|slc|xtra|var)_` and the resolved logic is not an `IWiredBox`, emit a warning and a counter. The silence becomes visible, with no database to query.

*Check to run on your side* (no MySQL in the audit environment):

```sql
SELECT d.name, d.logic, COUNT(*) AS boxes
FROM furniture_definitions d
WHERE d.name REGEXP '^wf_(act|cnd|trg|slc|xtra|var)_'
  AND d.logic <> d.name
GROUP BY d.name, d.logic
ORDER BY d.name;
```

---

## 5. The most expensive seam: closed

It is the one that produced `2ccb45b` and `7c2ad33`, and it was the only one whose authority was not in the repository. It has been since you supplied the client.

`TryNormalizeIntParams` (`FurnitureWiredLogic.cs:636-708`) is brutal, and rightly so:

```csharp
if (tailRule is null)
{
    if (proposed.Count != fixedRules.Count)
    {
        return false;   // -> ApplyWiredUpdateAsync returns false -> nothing is saved
    }
```

A **one**-unit gap between the number of declared rules and the number of parameters the client sends, and the entire box refuses to save. Not that one parameter: the entire box. And the refusal is mute, which I have now traced all the way: `UpdateAddonMessageHandler.cs:28-40` (and its five twins) does `return;` on `false` — no error composer, and above all **not** the `WiredSaveSuccessEventMessageComposer` it would otherwise send. The client receives nothing at all. `docs/habbo-specs/unknowns/medium/uk_91fc7e0d6b.yaml` records that what the official server answers in that case is **unknown**, so Vortex answers nothing.

### 5.1 Where the authority actually is (and why my first proposal was wrong)

The previous version of this report proposed wiring the count onto `WiredSurfaceAnalyzer`, which already reads `com/sulake/habbo/roomevents/wired_setup/*Codes.as`. **That was a mistake, and of the same family as the defect it claimed to fix**: `vortex-modern-client/.gitignore`, line 14, ignores `sources/`. The AS3 dump is never committed. A check hooked onto the `.as` files is therefore blind everywhere except on a machine where someone unpacked the client by hand — which is precisely the failure mode of `check-header-registry.mjs` and `check-wire-conflicts.mjs` (§7).

The committed authority is the TypeScript port: `packages/vortex-engine/src/habbo/roomevents/wired_setup/<family>/**/<Box>.ts`, where each box carries

```ts
override readIntParamsFromForm(): number[]
{
    return [this._effectId.value, this._priority.value, this._type.selected];
}
```

— literally the array that goes out on the wire. That directory is versioned, so the check works anywhere the client is cloned, CI included.

### 5.2 The shipped check

`scripts/hooks/check-wired-param-counts.mjs` (+ `wired-param-counts-baseline.json`). Plain node, same ratchet idiom as the others. It pairs boxes by `(family, code)` — the `Wired*Type` enum on the Vortex side, the `*Codes` constants on the client side — then compares `GetIntParamRules().Count` to the number of ints the form sends, accounting for a tail rule when there is one.

```
check-wired-param-counts: OK (95 boxes compared against the client, 51 whose client form
builds its array from the operator's selection, 15 with no client configuration class;
5 known disagreement(s) baselined; --skipped lists what was not compared).
```

Two subtleties had to be handled for the count to be right, and they are worth stating because they explain why this check was not trivial:

- **The array is not always a literal.** The six *Variable FX* displays — precisely those in `2ccb45b` — build theirs with `params.push(...)` in an inherited `writeIntParams` method, which the derived box overrides to add one more int. The check follows the inheritance chain with virtual dispatch (`super.` goes up, `this.helper()` restarts from the most derived class) and counts `Util.pushIntAsLong` as two.
- **A conditional push cannot be counted.** When a `push` is inside an `if`, a loop, a spread or a `concat`, the number depends on what the operator ticked: the check answers "unknown" and does not compare, rather than guess. That is what the 51 uncompared boxes mean; `--skipped` lists them.

### 5.3 The five disagreements, verified by hand on both sides

| Box | Client | Vortex | What it gives |
|---|---:|---:|---|
| `wf_trg_user_performs_action` | 1 | 0 | `UserPerformsAction.ts` always sends the action code; no rule declared |
| `wf_act_freeze_habbo` | 2 | 0 | `FreezeUser.ts` sends `[effect, cancel-on-teleport]`; no rule declared |
| `wf_slc_users_with_var` | 5 | 1 | inherits `VariableSelector.readIntParamsFromForm` (1 + 1 + a long over two slots + 1); a single rule declared |
| `wf_slc_remote` | 2 | 0 | `RemoteSelector.ts` sends `[type, count]`; no rule declared |
| `wf_xtra_mov_physics` | 4 | 0 | `MovePhysics.ts` sends four booleans; no rule declared |

Each of these five boxes **refuses its entire configuration, silently, on every save**. The player opens the box, sets it, saves, the window closes, nothing is written.

**`wf_trg_user_performs_action` is the most instructive of the five**, and it deserves its own paragraph, because it shows the pattern closing back on the person who had spotted it.

The class is deliberately empty, and its comment explains why:

> *"Deliberately inert: this client revision ships no configuration class for the code — `wired_setup/triggerconfs/` has one class per trigger and **none of them declares 16** — so the box cannot be given an action to watch for."*

**That claim is false.** `triggerconfs/UserPerformsAction.ts` exists, does return 16, and sends one int: the action code (`wave`/`blow`/…/`sign`/`dance`), plus a `stringParam` for the sign or dance index.

The cause of the error is visible in the client itself: the constant has no readable name. It is called `TriggerConfCodes.TRIGGER_CODE_16`, because in AS3 it is obfuscated as `_SafeStr_10352` and no tree carries its real name. **A search by name** — `USER_PERFORMS_ACTION`, `AVATAR_PERFORMS_ACTION` — **finds nothing and legitimately concludes "the client does not declare it".** A comparison by value finds it immediately. That is precisely what the shipped check does, pairing on `(family, code)` and never on a name.

And this box's two twins, which the client builds from the same form, **are implemented and correct**:

| | Client | Vortex | State |
|---|---:|---:|---|
| `wf_cnd_user_performs_action` (condition 32) | 1 | 1 | `Evaluate` written, rule declared |
| `wf_slc_users_byaction` (selector 9) | 1 | 1 | `SelectAsync` written, rule declared |
| `wf_trg_user_performs_action` (trigger 16) | 1 | **0** | empty, on a false premise |

So Vortex already knows how to read that parameter, in two places. The trigger is the only one of the three that was abandoned, and it was abandoned for a reason that does not exist. *Fix*: declare the rule, wire the event, and delete the comment — or, at minimum, correct the comment, because as written it actively discourages anyone who would want to finish the job.

Three clarifications that change the priority of the other four:

- **`wf_act_freeze_habbo` is the most expensive of the five**: its `ExecuteAsync` is written and works (it locks movement via `_ctx.Game.LockMovement`). The behaviour exists, it is simply unreachable. Its own class comment claims "*No int params*" — that sentence is the one the client contradicts. Declaring the two rules makes the box saveable; *honouring* the effect and the cancel flag is separate work, not to be confused with it.
- **`wf_slc_users_with_var` is an asymmetry case**: its twin `wf_slc_furni_with_var` does declare its 5 rules, and the check confirms it (`client=5 vortex=5`). Both boxes read the same client form; only one was updated. That is the exact signature of this audit's pattern.
- **`wf_xtra_mov_physics` and `wf_slc_remote` have no behaviour** beyond `WiredCode`: fixing the count alone would change nothing visible. They join §4.1.

All five are in the baseline with, each, a note saying what it is. The `_` entry of that file says it in so many words: *"Every entry here is an OPEN BUG, not accepted debt"*. The fingerprint includes both numbers (`client=5:vortex=1`), so if either moves, the entry becomes new again and the check blocks once more — baselining 21-against-22 does not cover 22-against-23.

### 5.4 The regression proof

That is the test that counts, because a check written after the fact always finds what it was shown. With **today's baseline**, run on the tree of the commit preceding `2ccb45b`:

```
check-wired-param-counts: 2 box(es) whose rule count disagrees with the client.
  wf_xtra_fx_levelling_progress    addon/1202  client=22  vortex=21
  wf_xtra_fx_number_display        addon/1205  client=22  vortex=21
exit=2
```

Exactly the two boxes that commit fixes, exactly the right delta, and nothing else. The bug would have been stopped before it was written.

A necessary honesty about scope: `7c2ad33` would **not** be caught by this check. That bug was not a count disagreement but a read beyond the fixed rules, on the runtime side — that is the tail-rule seam, not the count seam. This check closes the `2ccb45b` seam, which is the more expensive of the two, not both.

### 5.5 What this check almost made me say

Its first version reported **33 disagreements**, 30 of them of the form `vortex = client + 1`. The cause: it counted top-level commas, and a C# collection expression accepts a trailing comma — `[a, b, c,]` has three elements and three commas. A check written to count parameters was itself off by one, that is, struck by the exact bug it is looking for.

It stayed unpublished because `wf_act_give_effect` served as a control: I had read both its sides earlier and knew they were both 3, yet the script announced 4. None of the 33 was reported before the control went green. The 5 that remain were then re-read one by one, file against file, before entering this table.

The lesson is not anecdotal: it holds for any detector you add. **A check needs a known-good control before its output is worth anything.** The two *Variable FX* boxes and `wf_slc_furni_with_var` play that role here permanently, since they must stay at `22/22`, `21/21` and `5/5`.

### 5.6 What this check does not see

- **51 boxes** whose client form builds its array from what the operator ticked. Their arity legitimately varies; comparing a fixed number makes no sense. Those need a tail rule on the Vortex side, and it is that *presence* that should be checked, not the count — a natural extension of the check, not done.
- **15 boxes** with no configuration class on the client side: either Vortex-specific boxes, or a code that moved. `--skipped` lists them; they deserve a look, but are not bugs in themselves.
- **The meaning of the parameters.** The check compares numbers, not meanings. A box declaring the right number of rules in the wrong order goes green and misbehaves.


## 6. What is shipped, and the proof it works

Two checks. The second, `check-wired-param-counts.mjs`, is described in §5; this one covers the other six seams.

`scripts/hooks/check-inert-declarations.mjs` (+ `inert-declarations-baseline.json`).

Six seams, one pass, **0.9 seconds**, no dependencies (plain node, like the repository's seven other checks). Current output:

```
check-inert-declarations: OK (185 wired boxes, 86 variable readings, 561 handlers,
394 composers, 62 config keys; 56 known gap(s) baselined).
```

Same idiom as the existing checks: ratchet with a baseline, `--update` to accept a gap, `--all` to see everything, exit 2 on a **new** gap. Today's 56 gaps are in the baseline **with a note each**, two of them explicitly marked `OPEN BUG, not accepted debt` (the two inert boxes of §4.1): the baseline exists to allow immediate adoption, not to bury the work.

**The regression proof.** Run on the tree of the commit preceding the `482acb1` fix:

```
[wired-variable-flags]
  @position.x   declares CanWriteValue with no write behind it: the picker offers it,
                selecting it does nothing
  @type         declares CanWriteValue with no write behind it: …
```

Exactly the two readings that `482acb1`'s message describes, found in one second, when it had taken reading the client to notice them.

**Adoption**: one line in `Directory.Build.targets`, in the `VortexCloudFastCheck` target, next to the five checks already there.

```xml
<Exec Command="node scripts/hooks/check-inert-declarations.mjs"
      WorkingDirectory="$(MSBuildThisFileDirectory)" />
<Exec Command="node scripts/hooks/check-wired-param-counts.mjs"
      WorkingDirectory="$(MSBuildThisFileDirectory)" />
```

I did not add them, for two reasons. The target is red today for other reasons (§7) and grafting one more check onto it would not help while the barrier is bypassed. And this audit was to deliver a report and validation material **without touching production code**: a new script and its baseline are validation material, a line in the build target is not quite that any more. The decision is yours; the count check, for its part, exits 0 without the client and so will not break a machine that does not have it.

---

## 7. Why the pattern was inevitable, and stays so while the barrier is broken

Two causes, one structural, one circumstantial.

**The registration surface.** Adding a wired box requires touching, at minimum: the logic class and its attribute, the `WiredCode`, the parameter rules (whose count comes from the client), possibly `GetMaxVariableIds`, `GetAllowedFurniSources`, `GetAllowedPlayerSources`, `GetWiredContextSnapshots`, the admin dropdown, the catalog row, and the `logic` column in the database. Ten places, of which **only one** was checked mechanically (the dropdown).

And here is the fact that settles the question, because it rules out the easy answer ("we just need to be more careful"):

> `docs/walkthroughs/add-a-wired-box.md` exists. Its step 3 is titled "*Declare the parameter rules, even if they look redundant*" and warns, in bold: "*This is the one that bites, and it fails **silently on the operator's screen***". Point 4 of its checklist says: "*One `IWiredParamRule` per int the form sends, or a tail rule — **or the config is silently refused***."
>
> That text existed before 12 September. On 13 September, `2ccb45b` fixes two boxes that could not be saved for that exact reason. On 14 September, `7c2ad33` fixes a third.

The knowledge was written, in the right place, in bold, with the failure mode named. The bugs shipped anyway, twice, within forty-eight hours. **A checklist is a reminder for someone who thinks to re-read it; it is not a check.** That is the only conclusion this history allows, and it is why this audit's deliverable is a script and not one more paragraph.

The contrast with the room games points the same way, but from the other end: their walkthrough mandates the "balance keys in `ConfigKeyCatalog`" step and all their keys are there; pets and fishing, with no walkthrough, are not (§4.3). What decides is not care: it is whether a step is checked by a machine or not.

**The barrier is unplugged.** That is the beta report's QA-01 finding, still true at the time of this audit, and it is half the answer to "why don't we notice":

| Check | State today |
|---|---|
| `check-dashboard-capabilities.mjs` | exit 0 |
| `check-architecture-walls.mjs` | exit 0 |
| `check-logic-groups.mjs` | exit 0 |
| `check-header-registry.mjs` | **exit 2** (ceiling mode, ignores its own baseline) |
| `check-wire-conflicts.mjs` | **exit 2** ("the CLI output format changed, this check is blind") |
| `scripts/hooks/__test/run.mjs` | **exit 1** (3 self-tests failing) |

So `VortexCloudFastCheck` fails whatever you do, so commits go out with `--no-verify` (the messages of `4ca7c8a`, `482acb1` and `0455032` say so), so **none** of the checks — including those that work — runs before `main`. Writing new checks while the old ones are bypassed is pointless: the first thing to do is to get the three back to green.

Compilation and tests are otherwise healthy: `dotnet build Vortex.Cloud.sln` → 0 errors; full suite → 0 failures (~3,900 tests, same figure as on 14 September).

---

## 8. Proposed plan

**First of all — put the barrier back (1 day).** Repair the three broken checks, then wire `check-inert-declarations.mjs` into `VortexCloudFastCheck`. Without that step, everything else is decorative. Criterion: `dotnet build Vortex.Main/Vortex.Main.csproj -t:VortexCloudFastCheck` green, and CI green on the three OSes.

**Then, by increasing cost:**

1. **The 48 configuration keys** (1 h). One descriptor per key in `ConfigKeyCatalog.All`, then `--update` the baseline. Criterion: the check passes with 8 remaining gaps instead of 56.
2. **`SetRelationshipStatus` and `ChargeFirework`** (1 h). For the first, one `MapParser` line in `UsersMap.cs` — the id and the parser already exist. For the second, identify the id in the client or delete the triplet. Criterion: setting a relationship status from the client changes the displayed value.
3. **The two inert boxes** (0.5 to 2 days). `wf_xtra_mov_carry_users`: consume `_carryUserType` in the furni movement. `wf_slc_furni_with_var`: write `SelectAsync` and declare `GetMaxVariableIds() => 1`, or remove the box from the registry documenting why. Criterion: a test that places the box, configures it and checks the selection.
4. ~~**Seam 7**~~ — **done** (§5). What remains is to draw the five fixes from it: declare the missing rules of `wf_trg_user_performs_action`, `wf_act_freeze_habbo`, `wf_slc_users_with_var`, `wf_slc_remote`, `wf_xtra_mov_physics`, then `--update` the baseline so it drops back to zero. Count ~2 h for the first three (the rules are mechanical, the client says what to declare); the last two only make sense together with point 3, since they have no behaviour. Criterion: the `wired-param-counts-baseline.json` baseline contains an empty `known` list.
5. **The variable-arity boxes** (0.5 day). The 51 boxes of §5.6 need a tail rule, not a fixed count; extend the check to verify that a box whose client form is dynamic declares one. That is the remaining half of this seam.
6. **The silent wired fallback** (2 h). The five lines of §4.4, plus an alert on the counter.
7. **The dead code** (1 h). Delete the four unreachable triplets, or document them: an inert box that **says** it is inert is no longer a trap. With one reservation born of this audit: the comment must be verifiable, and re-verified. The one on `WiredTriggerHabboPerformsAction` asserts a client-side absence that does not exist (§5.3) — false documentation costs more than no documentation, because it closes the question.
8. **The walkthrough** (1 h). It is good and already covers the main trap; it is missing `GetMaxVariableIds` (the default of 0, which cost `0aed855` and still costs `wf_slc_furni_with_var`) and the admin dropdown entry. Add above all, at the head of the checklist, the line "the checks that verify this are `check-inert-declarations` and `check-wired-param-counts`": a checklist backed by a machine is followed, a checklist alone is not. And fix its step 3, which says to derive the parameter count "from the client" without saying where: it is `packages/vortex-engine/src/habbo/roomevents/wired_setup/**/<Box>.ts`, method `readIntParamsFromForm`.

---

## 9. Coverage and limits of this audit

**Analyzed mechanically, across the whole tree**: 185 wired boxes (parameters, behaviour), 86 variable readings (flags), 23 triggers (events), 561 handlers and 555 parsers (inbound chain), 394 composers (outbound chain), 62 configuration keys, 260 `[RoomObjectLogic]` keys.

**Analyzed by reading**: `FurnitureWiredLogic` (save path, normalization, hydration), the six family base classes, the four variable bands and their bases, the two inert boxes, the six unreachable handlers, `WiredSurface.cs`, `RoomObjectLogicProvider`, the SQL binding scripts.

**Not verified**:
- The wired **execution engine** itself (`Vortex.Rooms/Wired/Engine/**`: cycles, depth, scheduler, execution windows) — it has its own test suite and parity matrix (`docs/architecture-v4/acceptance-matrix.md`), which I did not cross-check.
- The **actual behaviour**: no MySQL, no run of the emulator or the client in this environment. The two inert boxes and the five count disagreements are established by reading the code on both sides, not by a played session. The client is read as a source, not executed.
- The official client's **AS3 dump**, absent here and never committed (`vortex-modern-client/.gitignore` l.14): `check-header-registry.mjs` and `check-wire-conflicts.mjs` stay blind, and the 22 boxes the client can configure with no Vortex implementation (`docs/completeness/generated/WIRED-BOXES.md`) were not recounted. Seam 7, for its part, no longer needs that dump (§5.1).
- An unexplored lead, flagged because it is worth a day: the TypeScript port also carries the id registry, `packages/vortex-engine/src/habbo/communication/HabboMessages.ts`, in the form `this._events.set(<id>, <Class>)` / `this._composers.set(...)`. That is a **committed** authority for what `check-header-registry.mjs` goes looking for today in an absent dump. I did not wire it up — it is an existing check, hence tooling code, outside the "do not modify" scope you set — but it is probably the shortest path to getting that check back to green (§7).
- The wired families **beyond their envelope**: the 53 actions, 45 conditions and 21 selectors were run through the detectors, not read one by one. A business-logic defect inside a correctly declared box would not be seen by this audit.
