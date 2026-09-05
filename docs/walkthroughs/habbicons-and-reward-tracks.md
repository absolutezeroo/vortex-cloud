# Habbicons and Reward Tracks

Two features the official client has always had and this server never answered. FC-2 called
Habbicons "9 obligations, an entire feature with nothing behind it"; reward tracks were two more
(`1376` claim, `1789` premium — the second one moves value).

This is the protocol they speak, the shape of the two domains, and how to add a campaign without
writing C#.

---

## 1. The protocol

Every id below was read from the target client's own registry,
`com/sulake/habbo/communication/_SafeCls_2046.as`, which has two independent tables:

- `_composers[<id>] = _SafeCls_X` — **client → server**, our `MessageEvent`.
- `_SafeStr_4546[<id>] = _SafeCls_Y` — **server → client**, our `MessageComposer`. `_SafeCls_Y` is a
  four-line wrapper whose constructor does `super(fn, _SafeCls_Z)`; **`_SafeCls_Z` is the real
  parser** and its `parse(IMessageDataWrapper)` is the wire order.

The same number means two different messages in the two directions. All twenty ids pass
`check-header-registry.mjs`.

### 1.1 Habbicons — client → server

| id | Message | Payload | Client call site |
|---|---|---|---|
| 272 | `GetHabbiconShopData` | — | `HabbiconController.getShopData()` |
| 1494 | `GetHabbiconInfo` | `int habbiconId` | `HabbiconController.getHabbiconInfo(id)` |
| 3980 | `BuyHabbicon` | `int habbiconId` | `HabbiconController.buyHabbicon(id)` |
| 3036 | `BuyHabbiconCollection` | `int collectionId` | `HabbiconController.buyHabbiconCollection(id)` |
| 662 | `ClaimHabbicon` | `int habbiconId` | `HabbiconController.claimHabbicon(id)` |
| 1808 | `FavouriteHabbicon` | `int habbiconId` | `HabbiconController.favoriteHabbicon(id)` |
| 75 | `UnfavouriteHabbicon` | `int habbiconId` | `HabbiconController.unfavoriteHabbicon(id)` |
| 1176 | `TriggerHabbicon` | `int habbiconId` | `HabbiconSelector.sendTriggerHabbicon(id)` |
| 1163 | `SendHabbicon` | `int chatId, int habbiconId, int confirmationId` | `messenger/MainView.onHabbiconSelected` |

### 1.2 Habbicons — server → client

| id | Composer | Payload | Client parser |
|---|---|---|---|
| 3728 | `UserHabbicons` | `int n`, n×(`int habbiconId`, `int state`), `int m`, m×`int recentId` | `_SafeCls_4256` |
| 2019 | `UserHabbiconStatusChanged` | `int habbiconId`, `int state` | `_SafeCls_4372` |
| 3765 | `HabbiconShopData` | `int n`, n×collection | `_SafeCls_4183` |
| 3714 | `HabbiconInfo` | one shop row | `_SafeCls_4081` |
| 1547 | `RoomUseHabbicon` | `int roomIndex`, `int habbiconId` | `_SafeCls_4246` |

**Collection block** (`_SafeCls_4498`): `int collectionId`, `string name`, `bool completed`,
`int rewardHabbiconId`, `int rewardState`, `int priceCredits`, `int priceActivityPoints`,
`int activityPointType`, `int n`, n×shop row.

**Shop row** (`_SafeCls_4487`): `int habbiconId`, `string name`, `int collectionId`, `int state`,
`int priceCredits`, `int priceActivityPoints`, `int activityPointType`.

> **Trap.** In `_SafeCls_4246` (`RoomUseHabbicon`) the getters are declared `habbiconId` first and
> `roomIndex` second, but `parse` reads **`roomIndex` first**. The parse order is the authority.
> Swapping them draws the wrong picture over the wrong avatar and nothing errors.

There is **no Habbicon-specific purchase result**. `HabbiconController` subscribes to the
catalogue's own `PurchaseOK` (1570), `PurchaseError` (1029) and `PurchaseNotAllowed` (2493)
alongside it, and `onPurchaseOk` is the only thing that closes its confirmation dialog. A Habbicon
purchase therefore answers with a synthetic `CatalogOfferSnapshot` carrying no products — the client
reads the block and ignores every field, but it does read it.

### 1.3 Habbicon states

From `HabbiconView.as`, which derives every flag it renders from one integer:

| value | meaning | client derives |
|---|---|---|
| 0 | not owned | `purchasable = state == 0 && hasPrice` |
| 1 | claimable | `claimable = state == 1` |
| 2 | owned | `owned = favorite \|\| state == 2` |
| 3 | owned + favourite | `favorite = state == 3` |

`HabbiconController` additionally treats 1, 2 and 3 as "the server keeps a row for this"
(`isStoredUserState`) and 1 → 2|3 as "a claimable reward was just claimed", which is what fires its
"new Habbicon" notification. Sending state 0 is how a Habbicon is **removed**: the client drops any
row whose state it does not consider stored.

The client's `HabbiconState` class also declares 4 and 5 (`REWARD`). **Nothing in the decompiled
tree reads either as a wire value**, so `Vortex.Primitives.Habbicons.HabbiconState` deliberately does
not have them. That is an unknown left explicit rather than guessed.

### 1.4 The Habbicon in a private conversation

`NewConsoleMessage` and `ConsoleMessageHistory` do not carry a string body. They carry a **tagged
union** (`_SafeCls_3241.parse`):

```
int kind
kind == 0 -> string text
kind == 1 -> int habbiconId
```

Exactly one arm is written, never both. A Habbicon in a conversation *is* a message, so it goes down
`IMessengerGrain.SendMessageAsync` with a `habbiconId` — the same friend check, block check, history
row and delivery a line of text gets — and `messenger_messages.habbicon_id` stores which.

### 1.5 Reward tracks — client → server

| id | Message | Payload | Client call site |
|---|---|---|---|
| 1376 | `ClaimRewardTrackPrize` | `string trackId, string prizeId` | `RewardTrackController.claimPrize(a, b)` |
| 1789 | `PurchaseRewardTrackPremium` | `string trackId` | `RewardTrackController.purchasePremium(track)` |

**That is the entire incoming surface.** The client never asks for the track list; it builds its
whole model from what the server pushes. So the list goes out at login and again whenever the answer
changes.

### 1.6 Reward tracks — server → client

| id | Composer | Payload | Client parser |
|---|---|---|---|
| 3794 | `RewardTracks` | `bool disabled`, `int n`, n×track, `bool reload` | `_SafeCls_2622` |
| 2017 | `RewardTrackProgress` | `string trackId, string taskId, int progressCount, int points` | `_SafeCls_3769` |
| 522 | `RewardTrackClaimResult` | `string trackId, string rewardId, int resultCode` | `_SafeCls_2641` |
| 58 | `RewardTrackPremiumPurchaseResult` | `string trackId, int resultCode, int points` | `_SafeCls_3538` |

Note `reload` is **last**, after the tracks — not beside `disabled`, which it reads like a pair with.

**Track block** (`_SafeCls_2628`):

```
string trackId
string theme
int    points
bool   hasPremiumConfig
  if hasPremiumConfig:
    double taskPointsBoost      <- big-endian 8 bytes; AS3 readDouble
    int    instantPoints
    int    costDiamonds
    int    costCredits
bool   premium                  (this player holds it)
bool   complete
bool   premiumComplete
int    taskCount   -> n × task block
int    prizeCount  -> n × prize block
```

> **Trap.** The premium block is **conditional**. Writing those four fields on a track with no
> premium tier misaligns everything after them, and the first thing to break is the task count being
> read out of the boost's bytes.

**Task block** (`_SafeCls_4299`): `string taskId`, `string actionType`, `string parameter`,
`int progressCount`, `bool premium`, `int n`, n×level.
**Level block** (`_SafeCls_4391`): `int requiredCount`, `int pointsReward`, `bool premium`.

**Prize block** (`_SafeCls_4204`): `string prizeId`, `int requiredPoints`, **`short`**
`productItemTypeId`, `string rewardTypeId`, `string extraParams`, `int rewardAmount`,
`bool premium`, `bool available`, `bool claimed`.

> **Trap.** `productItemTypeId` is a **short** while every other numeric field on the block is an
> int. Writing it as an int shifts the rest of the block by two bytes.

### 1.7 Result codes

These are the client's own numbering — it looks up
`reward_track.claim.notification.fail.<code>` and shows the localized line, so they must not be
renumbered. Taken from `external_flash_texts`:

| claim | | premium | |
|---|---|---|---|
| 0 | success | 0 | success |
| 1 | reward tracks are disabled | 1 | reward tracks are disabled |
| 2 | track not found | 2 | track not found |
| 3 | reward not found | 3 | not eligible for premium on this track |
| 4 | not eligible for this reward | 4 | premium is not configured for this track |
| 5 | not enough points | 5 | already own premium for this track |
| 6 | already claimed | 6 | configuration is invalid |
| 7 | failed to claim | 7 | not enough credits |
| 8 | premium is required | 8 | not enough diamonds |
| | | 9 | failed to unlock |

### 1.8 Reward kinds

`productItemTypeId` is the client's product-type numbering, read from
`ProductIconWidget.previewImage`, which switches on it to decide what the accompanying
`rewardTypeId` **string** means. `RewardKind` is on those exact values, so the serializer writes the
field straight out with no translation table to drift.

| value | kind | `rewardTypeId` is |
|---|---|---|
| -1 | none | — |
| 0 | wall item | wall item type id |
| 1 | floor item | furniture definition id |
| 2 | avatar effect | effect id |
| 4 | badge | badge code |
| 6 | bot | bot name (`extraParams` = figure) |
| 8 | currency | activity-point type: **-1 credits, 0 duckets, 5 diamonds** |
| 9 | chat style | style id |
| 10 | pet | pet type (`extraParams` = figure) |
| 12 | habbicon | habbicon id |
| 100 | entitlement | perk code — **outside the client's vocabulary on purpose** |

`Entitlement` renders as the unknown tile and is granted all the same. It is for anything that is a
permission rather than a thing.

### 1.9 Localization keys the content must match

The client builds these from ids, so a track id or task id is not a free choice:

```
reward_track.<trackId>.name / .desc / .info
reward_track.<trackId>.task.<taskId>.name / .desc / .hint.desc / .hint.button_text
reward_track_tasks_<actionType lowercased>        <- the task's artwork
habbicon_<code>_name
habbicon_collection_<code>_name / _description
```

> **`task_id` and `action_code` are two different vocabularies and must not be copied from one
> another.**
>
> - `task_id` is the **localization** stem. The client renders
>   `reward_track.<track>.task.<task_id>.name`, so it has to be one of the thirty ids in
>   `external_flash_texts`: `visit_rooms`, `change_outfit`, `chat_with_users`, …
> - `action_code` is the **artwork** key. `RewardTrackTaskRowView.as` builds the icon name as
>   `"reward_track_tasks_" + actionType.toLowerCase()`, so it has to be one of the thirty
>   `reward_track_tasks_*` embeds in `HabboWindowManagerCom.as`: `enter_other_users_room`,
>   `change_figure`, `chat_with_someone`, …
>
> The two lists overlap in name but not in content. Reusing the first for the second draws a blank
> square for every task and logs `ResourceManager: Asset not found` — which is how it was caught.
> `RewardTrackActions` holds the **artwork** vocabulary; the seed's task table pairs each with its
> own localization id.

Themes: `blue` (default), `orange`, `forest_green`, `red`, `cyan`. Anything else renders blue.

### 1.10 Two baselined wire conflicts

`check-wire-conflicts.mjs` flags `outgoing/UserHabbicons` and `outgoing/HabbiconShopData`. Both are
the documented **"parser delegates to a method"** false positive (shape 3 in that script's header):
the client's parser calls `helper.parse(wrapper)` inside its loop and the analyzer does not follow
it, so the client side counts one field where ours counts the block. Both serializers were read
field for field against `_SafeCls_4256` and `_SafeCls_4183` and match exactly.

---

## 2. The two domains

```
Vortex.Habbicons                       Vortex.RewardTracks
├── HabbiconCatalog        (cache)     ├── RewardTrackCatalog     (cache + action index)
├── HabbiconCollectionRules (pure)     ├── Progression/           (pure: stages, boost, view, gating)
├── Grains/PlayerHabbiconGrain         ├── Content/               (pure: the validator)
│     .cs / .Commerce / .Usage         ├── Rewards/               (granter per kind + pipeline)
└── Admin/HabbiconAdminService         ├── Grains/PlayerRewardTrackGrain
                                       │     .cs / .Progress / .Claims / .Premium
                                       ├── Events/                (the gameplay bridge)
                                       └── Admin/RewardTrackAdminService
```

The dependency runs **one way**: a reward-track prize may be a Habbicon, and nothing in
`Vortex.Habbicons` has heard of a track. The whole of that integration is
`HabbiconRewardGranter`, twelve lines.

### 2.1 The two flows

```
gameplay action succeeds
   -> domain event                     (PlayerEnteredRoomEvent, HabbiconUsedEvent, ...)
   -> RewardTrackSignal.SendAsync      (index says "nobody cares" -> stops here, no grain call)
   -> PlayerRewardTrackGrain.ProgressAsync
   -> TaskProgressRules.Apply          (pure: count moves, watermark decides what is owed)
   -> stage paid -> PremiumBoost.Apply -> track points
   -> milestone crosses -> prize becomes available
   -> player claims
   -> claim row written FIRST, then RewardGrantPipeline
   -> currency / furni / badge / habbicon / entitlement
```

```
habbicon acquired (bought, claimed, granted)
   -> ownership row
   -> HabbiconCollectionRules.IsComplete   (derived from ownership, never stored)
   -> collection complete -> bonus becomes Claimable
   -> player claims -> bonus owned
```

### 2.2 Why it is safe to repeat anything

| operation | what makes it once-only |
|---|---|
| task stage | `highest_paid_level_index` per task. A stage pays when it moves that number up, and only then. A jump past three thresholds pays three; the same event twice pays none. |
| prize claim | The claim row is inserted **before** the bundle is granted. Its unique index means a second attempt loses the insert, so the grant never runs and the caller is told `AlreadyClaimed`. |
| reward inside a bundle | Each carries an indexed commerce step key, so a retry re-runs what did not land and skips what did. |
| habbicon grant | A row that exists is reported "not new" and nothing is written. Backed by a unique `(player, habbicon)` index. |
| collection bonus | Goes through the same grant, so the second claim finds the row. |
| premium purchase | Refused with `AlreadyOwned` when the flag is set; the debit and the flag are one `ExecutePurchaseAsync`. |

### 2.3 The premium boost, exactly

Stored as **per-mille integers** (`1200` = 1.2×), applied with `(base * perMille + 500) / 1000` —
half-up, away from zero. `25 × 1.2 = 30` exactly; `25 × 1.15 = 28.75 → 29`.

A double would give 29 or 30 for the same content depending on how the operator's decimal rounded on
the way in, and a progression system that pays differently on two servers for the same work is a bug
nobody reproduces. The wire wants a double; that conversion happens once, in the serializer.

**Never retroactive.** The boost applies to points granted after premium is active. The one place
the engine looks backwards is deliberate: a premium *stage* a free player already passed leaves the
watermark where it is rather than writing it off, so buying premium later pays what they had already
earned.

### 2.4 Why only successful actions count

`PlayerChattingEvent` already existed and is a **cancellable pre-event** — it fires for a line a
behaviour then drops. Counting it would pay for words nobody heard, so `PlayerChattedEvent` was added
and published after the room has actually sent the line. The same rule shapes the rest: a room
Habbicon records nothing unless `RoomChatSystem` says it was shown, which means a muted player
clicking at a wall advances no task.

---

## 3. Adding a campaign

**No C#.** A track is rows in six tables; the dashboard writes them through
`IRewardTrackAdminService`.

1. Create the track (draft). Pick a theme, a completion policy and an unlock condition.
2. Add tasks. Each carries a **task id** (its localization stem), an **action code** from
   `RewardTrackActions` (its artwork key — see the warning in §1.9, these are not the same
   vocabulary), a **mode** (`Counter` / `Distinct` / `Absolute` / `Highest`) and its stages.
3. Add milestones and what each hands over. A milestone with several rewards is a bundle: one claim,
   all of them granted.
4. **Publish.** This runs the validator and refuses a track it reports problems on.

Add the matching `reward_track.<trackId>.*` keys to `external_flash_texts`, or the client shows raw
keys.

### 3.1 What the validator refuses

Each of these is a campaign that would look fine in the dashboard and be unplayable in the hotel:

- a milestone needing more points than the track can ever pay (free and premium ceilings computed
  separately);
- premium tasks or prizes on a track with no premium tier to buy;
- premium priced at nothing, or a boost below 1.0×;
- a task with no stages, stages that do not ascend, a stage requiring 0;
- a `Distinct` task pinned to one target, so it can never exceed 1;
- a prize that hands over nothing, or names a furniture with something that is not an id;
- claims closing before progress does, stranding points nobody can spend;
- a chapter unlocking from a track that does not exist, or **a cycle of chapters each waiting on the
  next** — the one that is invisible per track.

### 3.2 Adding an action code

Only when there is a **new gameplay signal**, and then it is one enum-ish constant plus one handler
in `RewardTrackEventHandlers`. The handler's whole job is to translate the event into
`(actionCode, amount, target)`; it knows nothing about tasks or tracks.

The rule the bridge exists to enforce: **subscribe to the event raised after the action succeeded**.
If the only event is a pre-event or a packet arrival, add the post-accept one first.

### 3.3 What the Introduction Track seeds, and what it leaves out

`Vortex.Database/Seeds/reward_track_introduction.sql` seeds **15 of the client's 30 introduction
tasks** — the ones an accepted gameplay action raises today. The other fifteen (`go_swimming`,
`grab_drink`, `pet_a_pet`, `feed_pet`, `level_pet`, `use_teleport`, `close_love_lock`,
`publish_picture`, `follow_friend`, `send_messenger_invite`, `set_relationship_status`,
`replenish_respect`, `use_furniture`, `rotate_furniture`, `place_builders_club_furni`) have no domain
event behind them yet.

Seeding them would ship tasks that can never advance — a bar that never moves is worse than a task
that is not there. Each becomes **one row plus one event handler** the day its signal exists.

### 3.4 Habbicon ids are the asset pack's

The client resolves a Habbicon's artwork by id from its own `habbicons.json` manifest, which is not
in this repository. The seeded ids are 1..33 in the order the codes appear in the official texts. If
the real pack numbers them differently, **every picture in the album is the wrong one** — the codes
are the anchor, the ids are ours, and an operator installing a real pack has to align them.

---

## 4. Configuration

| key | default | what it does |
|---|---|---|
| `reward_tracks.enabled` | `true` | Off sends the client its own disabled flag, which hides the feature rather than showing an empty list. |
| `habbicons.recent_limit` | `10` | How many recents the quick row keeps. The client caps its own at 10. |
| `habbicons.use_cooldown_ms` | `500` | Minimum gap between one player's uses. Separate from chat flood control, which does not cover private conversations. |

Client-side, `habbicons.enabled` in `external_variables` gates the whole hub. The client checks it
before sending anything, so a hotel with it off never reaches this code.

---

## 5. Files

| you want to | look at |
|---|---|
| understand the stage arithmetic | `Vortex.RewardTracks/Progression/TaskProgressRules.cs` |
| understand the boost | `Vortex.RewardTracks/Progression/PremiumBoost.cs` |
| see what the client is sent | `Vortex.RewardTracks/Progression/TrackViewBuilder.cs` |
| add a reward kind | `Vortex.RewardTracks/Rewards/RewardGranters.cs` + one line in `RewardTracksModule` |
| change what "complete" means | `Vortex.RewardTracks/Progression/TrackGatingRules.cs` |
| see collection completion | `Vortex.Habbicons/HabbiconCollectionRules.cs` |
| check the wire | `Vortex.Revisions/Revision20260701/Serializers/{Habbicons,RewardTracks}/` |
| read the content | `Vortex.Database/Seeds/{habbicons,reward_track_introduction}.sql` |
