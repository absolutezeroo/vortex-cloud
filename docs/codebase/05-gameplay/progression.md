# Progression

## Purpose

Achievements, quests, daily tasks, polls and prizes — and the one design decision that separates a
reward you can lose from one you cannot.

## The split-by-stakes rule

`Vortex.Progression/Grains/PlayerAchievementGrain.cs` — the class doc states it, and it is worth
copying:

| Kind of change | Persistence |
|---|---|
| **plain progress** | `state.Progress = …; state.Dirty = true` — **memory only**, batched by a timer |
| **a level-up** | `PersistAsync` writes the row **synchronously**, and returns `false` on failure with the state untouched — the method then **returns early** |

So no badge and no currency is ever granted for a level the database refused.

```csharp
// ProgressCoreAsync — the guard
if (!await PersistAsync(...)) return;    // nothing below this line runs
await ApplyLevelUpsAsync(...);           // badge → currency → score → composers → event
```

The flush timer runs at `AchievementConfig.ProgressFlushIntervalMs` (5000 ms),
`MaxDirtyProgressPerFlush` = 100, rows **stay dirty on failure** so the next tick retries, and
`OnDeactivateAsync` flushes.

> Consequence worth knowing: **daily achievements gate on `state.LastProgressAt?.Date == now.Date` in
> memory**, because the row now lags behind.

## The level-up sequence

`ApplyLevelUpsAsync`, in order:

```
RemoveBadgeAsync(previous)
  → GrantBadgeAsync(final)
    → per level: wallet.GrantCurrencyAsync(CurrencyRewardRules.KindFor(rewardType), amount)
      → IPlayerGrain.AddAchievementScoreAsync
        → HabboAchievementNotificationMessageComposer + AchievementsScoreEventMessageComposer
          → AchievementLevelUpEvent      ← last
```

`CurrencyRewardRules.KindFor(rewardType)`: negative = Credits, otherwise the activity-point type.
Shared by achievements, quests and daily tasks.

> **Every runtime grant call site discards `GrantCurrencyAsync`'s `bool`.** A grant to a currency with
> no enabled `currency_types` row is a logged no-op that the player never hears about.
> → [Economy overview](../06-economy/overview.md)

Wire shape: `AchievementData` is a 14-field cumulative block, and **level = completed + 1**.

## Quests and daily tasks

| Grain | Key | State |
|---|---|---|
| `QuestManagerGrain` `[KeepAlive]` | `"global"` | quest definition cache |
| `PlayerQuestGrain` | player id | none — DB per call |
| `PlayerDailyTaskGrain` | player id | none — reads `DailyTasks.AsNoTracking()` per call |
| `CommunityGoalGrain` `[KeepAlive]` | `"global"` | `_totalScore` — a genuine hotel-wide running total |

`player_daily_tasks (player_id, task_id, assigned_on)` is unique — **the date is part of the key**,
which is what makes a task re-assignable tomorrow.

Because `PlayerDailyTaskGrain` and `PlayerBadgeGrain` query per request, admin edits to their
definitions are class (a): a pure DB write with no cache to chase. That claim was independently
verified. → [Dashboard operations](../08-dashboard/operations.md)

## Replay-safe event consumption

Progression consumes economy events, and a relayed event can arrive twice. The guard:

```csharp
// CommerceReplayGuard.FirstDeliveryAsync(journal, operationId, consumer, ct)
//   writes a  relay:<consumer>  receipt
```

Each consumer passes **its own name**, so two consumers of one event dedupe independently. Used by
`QuestCatalogPurchaseHandler` and the daily-task equivalent.

→ [Catalog purchase](../flows/catalog-purchase.md)

## Polls and prizes

| Grain | Key | Role |
|---|---|---|
| `PollManagerGrain` `[KeepAlive]` | `"global"` | survey tree cache |
| `PlayerPollGrain` | player id | per-player survey state |
| `PrizePoolManagerGrain` `[KeepAlive]` | `"global"` | weighted prize pool cache |
| `PlayerPrizeGrain` | player id | granting and audit |

Prize pools carry two deliberate schema exceptions: `prize_pool_entries` and `prize_pool_bindings`
have a plain definition id with **no FK** (effect and club prizes legitimately leave it 0, and a hotel
may bind an id its furnidata has not shipped), and `player_prize_claims` cascades on **both** sides so
retiring a pool clears "already taken" and the event can be re-run.

## The progression delete policy

One rule across the whole schema, spelled out in `VortexDbContext.OnModelCreating`:

> **A definition owns its children (`Cascade`); player progress cascades with the player and is
> `Restrict` against the definition.**

Editing or retiring content never silently destroys what people already earned.
→ [Ownership boundaries](../07-database/ownership-boundaries.md)

## Two known risks

Both are pay-then-record shapes, on [Transactions](../06-economy/transactions.md):

- **`PlayerVaultGrain.ClaimCategoryAsync`** grants each reward (each its own commit) and only then
  deletes the reward rows. A failed delete lets the player claim again.
- **`PlayerNftClaimsGrain.ClaimAllAsync`** does the same **deliberately**, with a comment choosing
  *"an unclaimed prize is recoverable where a consumed one is not"*.

## Configuration

| Key | Default |
|---|---|
| `Vortex:Achievements:ProgressFlushIntervalMs` | 5000 |
| `Vortex:Achievements:MaxDirtyProgressPerFlush` | 100 |

**Undeclared, defaults-only:** `Vortex:Quests:{DailyTaskCount, DailyBonusTaskCount,
CommunityGoalHallOfFameSize}` are read raw from `IConfiguration` and appear in no config class.

## Known unknowns

- **Unverified:** `PlayerBadgeGrain`, `PlayerPrizeGrain`, `PlayerPollGrain`, `CommunityGoalGrain` and
  `PrizePoolManagerGrain` were outlined rather than read line by line. The badge paths reachable from
  the achievement flow *were* read.

## Sources

- `Vortex.Progression/Grains/PlayerAchievementGrain.cs` — the split-by-stakes doc, `ProgressCoreAsync`, `ApplyLevelUpsAsync`
- `Vortex.Progression/Grains/{PlayerQuestGrain,PlayerDailyTaskGrain,CommunityGoalGrain,QuestManagerGrain,PollManagerGrain,PrizePoolManagerGrain}.cs`
- `Vortex.Progression/Quests/Events/{QuestProgressEventHandlers,DailyTaskProgressEventHandlers}.cs`
- `Vortex.Primitives/Players/Wallet/CurrencyRewardRules.cs`
- `Vortex.Primitives/Commerce/CommerceReplayGuard.cs`
- `Vortex.Progression/Configuration/AchievementConfig.cs`
- `Vortex.Database/Context/VortexDbContext.cs` — the progression delete policy
