# Readability and cleanliness audit

- **Revision**: `6666f2d` on `claude/vortex-cloud-beta-audit-apl5gn`.
- **Date**: 2026-09-15.
- **Nature**: read-only audit. No code was modified.
- **Method warning**: a first pass of this report scored each axis on the basis of **a single measurement** per axis. Several of its conclusions were wrong. §5 lists what the deep pass corrected, because it is the most useful result of this audit: **on this repository, a single measurement almost always lies**, and §4 explains why.

---

## 1. The one-page answer

**The repository is clean in form, and uneven in substance.**

The form is excellent and that is not a polite compliment: 84% of files are under 100 lines, comment density is 13% of which 11% is XML documentation, there are only **two** commented-out code blocks in 403,000 lines, **four** real debt markers, and the architecture walls hold (7 checked, 0 leaks).

The substance less so, and for a reason that has nothing aesthetic about it:

> **`partial`, assembly scanning and splitting into small files make three things invisible: the real size of a class, the real usage of a type, and the real duplication of a declaration.** These are exactly the three blind spots that produced the bugs in the previous audits.

The three facts that show it:

1. `RoomGrain` is **6,865 lines across 31 files** and exposes **211 public methods**, of which **177 have a real body**. You never see its size.
2. **Six distinct mechanisms register types by assembly scanning.** Deleting an "unused" type compiles perfectly and silently breaks a feature. Any dead-code analysis is therefore unusable here — including Qodana's 397 "unused" findings.
3. **173 blocks of 12 lines are duplicated across 3 files or more.** In `Vortex.Rooms`, these are wired source declarations copied verbatim across 8 to 9 boxes — **the exact vehicle** of the bug documented in the wired audit, where one copy diverged with nothing comparing them.

It is the same pattern as everywhere else in this project: *a rule applied here, absent next door, and nothing that compares the two.*

---

## 2. What is healthy, measured

| Measure | Value |
|---|---:|
| Hand-written code (excluding migrations, `obj/`, `bin/`) | 402,775 lines / 6,324 files |
| Files ≤ 100 lines | **5,321 (84%)** |
| Files > 1,000 lines | **15 (0.24%)** |
| Comment density | **13%**, of which **11% is `///`** (39,320 lines) |
| Commented-out code blocks | **2** |
| Real TODO/FIXME markers | **4** |
| Architecture walls | 7 checked, **0 leaks** |
| Handlers touching `VortexDbContext` | **0** — the hard rule in `CLAUDE.md` holds |
| Formatting | csharpier 1.2.6 enforced by hook, clean tree |
| Documentation | 151 files, 25,463 lines |

**The comments deserve a separate mention.** They almost always explain *why*, naming the bug avoided:

> "Consumed before the credits exist, for the reason the crackable is: the reverse order turns one coin still standing on the floor into as many payouts as it can be clicked."

That is decision documentation, and it has direct operational value: it is what let me, in the security audit, tell an oversight apart from a deliberate choice.

**The 119 "TODO"s are not TODOs.** 115 are the same line — `// TODO: add properties if/when identified` — on composers whose official payload is unknown. That is the specs doctrine applied, not debt. **Four** remain, one of which is a `// TODO hmm`.

---

## 3. What deserves rework

### 3.1 Two god classes that `partial` conceals

| Class | Lines | Files |
|---|---:|---:|
| `RoomGrain` | **6,865** | 31 |
| `DashboardEndpoints` | **6,369** | 36 |
| `RoomPetSystem` | 3,481 | 8 |
| `PlayerGrain` | 2,049 | 6 |

Plus `WebApiEndpoints.cs`: **2,121 lines in a single file**, 45 endpoints, with no `partial` to excuse it.

The point about `RoomGrain` is not its raw size but its composition: **211 public methods, of which only 34 delegate** to a module or a system. **177 have a real body.** The decomposition exists (`SecurityModule`, `FurniModule`, `MapModule`, `PetSystem`…) but most of the logic stayed in the grain.

`RoomGrain` is an Orleans grain with turn-based concurrency: every public method is an entry point serialized on the same activation. 211 is a surface nobody holds in their head — and it is precisely the surface the security audit had to inventory by hand for lack of any way to compute it.

*Direction*: family by family, never in one go. Move `RoomGrain.Settings.*` to a `RoomSettingsSystem` modelled on `RoomPetSystem`, which proves the pattern already works here.

### 3.2 Two hundred and sixty-one methods of 80 lines or more

My first pass had measured long methods **only inside `RoomGrain`**, found 8, and concluded everything was fine. Across the whole tree, excluding type declarations and the 17 registration tables in `Vortex.Revisions` (which are long by nature):

```
   481  MapUser                   Vortex.WebApi/Hosting/WebApiEndpoints.cs:442
   418  HandleAsync               Vortex.PacketHandlers/Handshake/SSOTicketMessageHandler.cs:75
   347  LooksLikeMappingEntry     Vortex.Specs/Yaml/YamlReader.cs:157
   346  SettleContractAsync       .../WiredTrading/WiredTradeSettlement.cs:101
   289  ProcessRollersAsync       .../Systems/RoomRollerSystem.cs:36
   258  OnModelCreating           Vortex.Database/Context/VortexDbContext.cs:421
   237  ApplyWiredUpdateAsync     .../Wired/FurnitureWiredLogic.cs:313
```

**`SSOTicketMessageHandler` deserves to be named**: a 750-line file, including a **418-line** `HandleAsync` — while `CLAUDE.md` requires "keep packet handlers orchestration-only". It is the login handler, therefore the most critical on the server, and the only place where the security audit had to read 400 lines straight to answer "what happens at login?".

Five handlers exceed 150 lines; the other 554 are lean. Here again: the rule exists, it holds almost everywhere, and nothing flags the five exceptions.

### 3.3 Duplication is three times wider than my first measurement

First pass: "Duplication: 18 handlers out of 383 in identical families — that's fine." Real measurement, 12-line normalized sliding window over **all** hand-written C#:

```
  identical blocks in >= 3 distinct files: 173
    326 occurrences  Vortex.PacketHandlers
    217 occurrences  Vortex.Rooms
    147 occurrences  Vortex.Rooms.Tests
```

And the content matters more than the count. In `Vortex.Rooms` (44 blocks), what gets duplicated is the **wired source declarations**:

```csharp
public override List<WiredFurniSourceType[]> GetAllowedFurniSources() =>
    [[ WiredFurniSourceType.SelectedItems, WiredFurniSourceType.SelectorItems,
       WiredFurniSourceType.SignalItems,   WiredFurniSourceType.TriggeredItem ]];

public override List<WiredPlayerSourceType[]> GetAllowedPlayerSources() =>
    [[ WiredPlayerSourceType.TriggeredUser, WiredPlayerSourceType.SelectorUsers,
       WiredPlayerSourceType.SignalUsers ]];
```

copied verbatim across 8 to 9 boxes (`WiredActionGiveVariable`, `WiredActionRemoveVariable`, `WiredAddonSelectorFilter`, `WiredAddonVariablePlaceholder`, `WiredAddonVariableSortFilter`…).

**This is not cosmetic duplication.** It is the delivery mechanism of the bug the wired audit documented: `wf_slc_users_with_var` declares 1 parameter rule where its twin `wf_slc_furni_with_var` declares 5. When a declaration is copied eight times, the copy that diverges is invisible. A shared constant (`WiredSources.StandardFurniAndPlayers`) would make the exception visible by construction.

### 3.4 One hundred and four unbounded text columns

Out of 273 `string` properties on the entities, **104 (38%) have neither `[MaxLength]` nor `[StringLength]`**, spread across 53 entities. In MySQL, that is `longtext`. Qodana flags it 95 times.

The concrete case:

```csharp
// Vortex.Database/Entities/Room/RoomEntity.cs:23
[Column("name")] public required string Name { get; set; }   // no bound

// Vortex.Rooms/Grains/RoomGrain.Settings.cs:77
entity.Name = update.Name;                                    // comes off the wire, as is

// Vortex.Rooms/RoomService.Create.cs:94
Name = trimmedName,                                           // .Trim() only
```

A room name can therefore reach the size of a frame — 64 KB — and be stored as is. While the same repository writes, two files further on:

```csharp
entity.Name = name.Length > 100 ? name[..100] : name;   // RoomAdvertisementService.cs:47
return trimmed.Length > 25 ? trimmed[..25] : trimmed;   // the tags, RoomGrain.Settings.cs:660
```

The rule is written twice and missing on the most exposed of the three fields.

### 3.5 Fifty-three guards the contract says are impossible

```csharp
if (ctx is null || ctx.PlayerId <= 0 || ctx.RoomId <= 0 || message.Id <= 0)
```

`Nullable` is `enable` in `Directory.Build.props` and `MessageContext` is non-nullable: the contract says `ctx is null` cannot happen. **53 handlers out of 559** carry that test; 506 do not. Harmless — but it is a third variant of the same symptom: a defensive idiom applied to 9% of cases, with no rule deciding.

*(Nuance: the pipeline's non-generic dispatch could theoretically bypass nullability analysis. So I cannot say "dead code", only "the contract says it cannot happen".)*

### 3.6 A 37 MB generated artifact in git history

```
   36.9 MB  docs/qodana.sarif.json
    3.2 MB  tools/catalog_converter/data/asset_logic.json
    0.9 MB  Vortex.Database/Seeds/furni_logic_bindings.sql
```

`docs/qodana.sarif.json` is a **generated** analysis report, versioned, larger than all the rest of the repository's text put together. Every regeneration adds a full copy to history, which every clone carries forever. Its content is useful — it is what put me on the trail of the unbounded columns — but its place is a CI artifact.

### 3.7 Thirty obfuscated names, and large Svelte components

**30 `class_NNNN*.cs` files** in `Vortex.Revisions` (12 in `Help`, 4 in `Clothing`…), obfuscated AS3 names carried over as is. The choice is defensible — inventing a false semantic name would be worse — but those thirty carry no explanation, while the client's TypeScript port systematically documents its own ("*Name derived: the AS3 class is obfuscated as `_SafeCls_4182`*"). One line would be enough to turn an oddity into a decision.

**On the dashboard side** (157 files, 50,021 lines): five Svelte pages between 1,244 and 1,933 lines, `CollectiblesPage.svelte` in the lead. Same symptom as `RoomGrain`, without `partial` to hide it.

---

## 4. Why a single measurement lies on this repository

This is the most useful conclusion of this audit, and it holds beyond cleanliness.

**Six distinct mechanisms register types by assembly scanning**:

```
  Vortex.Runtime/AssemblyProcessing/AssemblyExplorer.cs     handlers, behaviours
  Vortex.Signals/SignalTranslatorFeatureProcessor.cs        ISignalTranslator<>
  Vortex.Rooms/Providers/RoomObjectLogicProvider.cs         [RoomObjectLogic]
  Vortex.Dashboard.API/Hosting/DashboardWebHost.cs
  Vortex.Database/Extensions/ModelBuilderExtensions.cs
  Vortex.Database/Commerce/CommerceRelayService.cs
```

Direct consequence: **a type can be named nowhere and still be indispensable.** Of 1,375 public types whose name appears in no other file, virtually all are alive — handlers resolved by scanning, tests discovered by xUnit reflection, request records bound by ASP.NET, signal translators, wired context variables.

Three practical consequences:

1. **Qodana's 397 "unused" findings** (259 `UnusedAutoPropertyAccessor` + 138 `NotAccessedPositionalProperty`) are not usable as is. I caught a false positive among them myself during the security audit: Qodana declares "*Class `RateLimitBehavior` is never used*" when it is the rate limiter for all inbound traffic.
2. **A "let's delete what is not referenced" cleanup is dangerous here**: it compiles, and it breaks silently. That is, once again, exactly this project's defect class.
3. **Static-analysis reports must be verified case by case.** Of the 4 Qodana categories I opened: `UnlimitedStringLength` (95) is **real and serious**; `ConditionIsAlwaysTrueOrFalse` (129) is real but benign (§3.5); `UnusedAutoPropertyAccessor` is a mix, some cases of which are **documented as deliberate** in the code; `AccessToDisposedClosure` (32) is a **systematic false positive** on the `await using` + EF lambda idiom — the two cases I opened, in two different files, invoke the lambda *inside* the `using` scope.

---

## 5. What the deep pass corrected

Published because it says which figures deserve trust.

| First pass (one measurement per axis) | After verification |
|---|---|
| "Duplication: fine — 18 handlers out of 383" | **173 blocks** duplicated across ≥ 3 files, 44 of them in `Vortex.Rooms` — and those are the wired declarations, direct cause of an already documented bug |
| "Dead code: excellent — 2 blocks" | Correct for commented-out blocks, but I had not looked elsewhere: **261 methods ≥ 80 lines** |
| Long methods measured in `RoomGrain` only (8 found) | **261** across the tree, including a **418-line** `HandleAsync` at login |
| Qodana: 2 categories out of 10 opened | 4 opened; a whole one (`AccessToDisposedClosure`) turns out to be noise |
| Unused types: not measured | 1,375 candidates, **virtually all alive** — and *that* is the result |
| "Handlers: orchestration-only respected" | True for `VortexDbContext` (0 violations), false for size: 5 handlers ≥ 150 lines |

Two measurement errors were also caught and fixed along the way: my commented-out-code detector ignored `/* */` blocks (it reported 0 when there are 2), and my long-method detector counted primary constructors as methods (it reported `PluginManager` at 823 lines, which is the class).

---

## 6. Suggested order of rework

1. **The string bounds** (0.5 d) — `[MaxLength]` on the 104, truncation on the room name. Smallest effort, and it closes an unbounded input before reopening.
2. **Get the SARIF out of git** (10 min) — `.gitignore` + CI artifact.
3. **Factor out the duplicated wired declarations** (0.5 d) — shared constants for the standard source sets. This is not comfort: it is what makes the diverging box visible.
4. **Split `SSOTicketMessageHandler`** (0.5 d) — 418 lines on the login path, against the repository's own rule.
5. **`RoomGrain`, family by family** (ongoing) — start with `Settings` (146 lines in a single method). `RoomPetSystem` is the model.
6. **The two dead blocks and the 30 `class_NNNN`** (1 h) — delete, or write in place why they stay.

---

## 7. Limits

- **Nothing was executed**: no build, no tests, no coverage. The test/production ratio (85,324 / 317,509) is a **line volume**, not coverage. A project with no test project is not necessarily uncovered — its logic may be covered from `Vortex.Rooms.Tests` — and I did not measure it. The only hard fact is that `Vortex.PacketHandlers.Tests` contains **one file** covering the pure function of a single handler out of 559, and that **25 projects have no test project**, including `Vortex.Catalog`, `Vortex.Inventory`, `Vortex.Marketplace`, `Vortex.Networking` and `Vortex.Protocol`.
- **Duplication is measured over 12 normalized lines**: it sees neither shorter blocks, nor logical duplication rewritten differently, nor the Svelte front end (measured by volume only).
- **The 2,466 Qodana findings**: 4 categories out of 10 opened, a few cases each. The other 6 are not judged.
- **The quality of the comments is a judgement** based on a broad but not exhaustive reading; the density, on the other hand, is measured.
- **The dashboard front end** received no code, accessibility or dependency review.
- The hooks self-test reports **2 failures**, both tied to the header registry, which needs the AS3 dump missing from the client repository. The formatting check passes.
