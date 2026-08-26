# Social

## Purpose

Friends, messenger, guilds, forums and guides — and the presence fan-out that makes a friend list
show the truth.

## Messenger

`Vortex.Social/Grains/MessengerGrain.cs` plus `.Friends`, `.Messaging`, `.Presence` partials, keyed by
player id.

**Hydrate-on-activate.** Six collections come into memory: `_friends`, `_incomingRequests`,
`_blockedIds`, `_ignoredIds`, `_categories`, `_pendingDeliveredIds`.

### The activation fan-out

```csharp
// MessengerGrain.HydrateAsync — one IsOnlineAsync per friend, all at once
await Task.WhenAll(friends.Select(f => presence(f).IsOnlineAsync()));
```

The comment records the bug it fixed: **presence only ever arrived via a *change* notification**, so a
friend who was already connected when you logged in stayed offline in your list forever.

> It is one grain call per friend, unbounded by anything but `messenger.max_friends` (default 300).
> → [Performance](../10-operations/performance.md)

### How a presence change reaches a friend

`MessengerGrain.Presence.cs` — `NotifyOnlineAsync` / `NotifyOfflineAsync` iterate `_friends` and
**fire-and-forget** (`LogAndForget`) `friendGrain.NotifyFriendPresenceChangedAsync(self, online,
figure, motto, CancellationToken.None)`.

The receiving grain updates its `_friends` entry — lazily creating it with a
`IPlayerDirectoryGrain.GetPlayerNameAsync` lookup for a friend added while it was offline — and
fire-and-forgets a `FriendListUpdateMessageComposer` with a single
`FriendListUpdateActionType.Updated` entry.

N grain calls, sequential enqueue, none awaited. That is deliberate: a slow friend must not hold up
your login.

### Friend mutations

| Operation | Shape |
|---|---|
| `SendFriendRequestAsync` | limit checked on **both** sides (self from memory, target via `CountAsync`), duplicate check, insert, fire-and-forget `ReceiveFriendRequestAsync`, `FriendRequestSentEvent` |
| `AcceptFriendRequestsAsync` | **both directions of `MessengerFriendEntity` plus the request `RemoveRange` in one `SaveChangesAsync`**. `selfOnline` resolved once from presence rather than assumed |
| `RemoveFriendsAsync` | one batched `ExecuteDeleteAsync` covering both directions, then per-friend events under `Task.WhenAll`, plus fire-and-forget `NotifyFriendRemovedAsync` |

Every messenger relation table is unique on the **ordered pair** `(player_id, other_id)` — which is
what makes "add if absent" safe. → [Entities](../07-database/entities-and-relationships.md)

### Offline messages

`DeliverOfflinePendingMessagesAsync` skips ignored senders and batches `Delivered = true` writes
through `_pendingDeliveredIds` on a `MessengerConfig.DeliveredFlushIntervalMs` (10 s) timer — the
timer-flush pattern for housekeeping writes.

`messenger_messages` carries **both** `(receiver, sender, timestamp)` and
`(sender, receiver, timestamp)` indexes so a conversation reads efficiently in either direction.

### A known inconsistency

`BlockUserAsync` and `IgnoreUserAsync` mutate the in-memory set **first**, then `return` early if
either player entity is missing — leaving the set and the table disagreeing for the life of the
activation. → [Transactions](../06-economy/transactions.md)

## Guilds

| Grain | Key | State |
|---|---|---|
| `GroupGrain` (5 partials) | group id | **none** — its own doc: *"holds no in-memory state … so it is a serialization point per guild rather than a cache"* |
| `GroupDirectoryGrain` `[KeepAlive]` | `"global"` | none — creation and my-groups |
| `GroupForumGrain` | group id | none — threads, posts, moderation |

Guild creation debits through `ExecutePurchaseAsync` and raises a **`CatalogPurchasedEvent`**, so
guild creation shows up in purchase analytics.

### The room link

`groups.room_id ↔ rooms.group_id` is modelled as **two independent one-directional relationships**,
both `Restrict`, so MySQL never builds a cascade cycle. Dissolution is three statements: detach, then
soft-delete.

The uniqueness lives on `rooms (group_id)` — the **nullable** side — because MySQL permits repeated
NULLs, so dissolving a guild frees the room. A unique index on `groups.room_id` would be permanently
blocked by the soft-deleted guild row.
→ [Ownership boundaries](../07-database/ownership-boundaries.md)

### Guild rights are live room state

After a roster or settings change, `GroupGrain` calls `IRoomSecurity.RefreshGroupMembershipAsync`,
which **re-reads `rooms.group_id` from the DB** rather than trusting the cached snapshot — a dissolved
guild would otherwise keep granting build rights — then re-pushes the controller level for every
affected present player.

→ [Room architecture](../04-rooms/room-architecture.md)

Forum thread and post state is a **byte** on the wire, not an int.

## Guides

`GuideDirectoryGrain` — `"global"`, `[KeepAlive]`, six dictionaries, a 5 s sweep timer, and
**deliberately no persistence**:

> its interface doc states that persisting on-duty status would be wrong — it describes a live client
> and **must not survive a restart**.

`PlayerDisconnectedEvent` is consumed by `Vortex.Social/Events/GuideDisconnectHandlers.cs`, which
clears duty, ends the guide session and notifies the partner with `EndReason = 0`.

`PlayerQuizGrain` handles help quizzes with server-side grading.

## Where social state lives

| State | Live owner | DB | Coherence |
|---|---|---|---|
| friends, requests, blocks, ignores | `MessengerGrain` (hydrated) | `messenger_*` | write-through, except the block/ignore early-return above |
| delivered flags | `MessengerGrain` queue | `messenger_messages` | 10 s timer flush |
| guild membership and ranks | **`RoomGrain`** (`GroupMemberRanks`) for authorization; `GroupGrain` owns nothing | `group_members` | refreshed by an explicit grain call |
| guide duty roster | `GuideDirectoryGrain` | **nothing** | ephemeral by design |

## Configuration

| Key | Default | Where |
|---|---|---|
| `messenger.max_friends` | 300 | `IServerConfigGrain` |
| `messenger.message_history_limit` | 50 | `IServerConfigGrain` |
| `Vortex:Messenger:DeliveredFlushIntervalMs` | 10 000 | `MessengerConfig` |
| guild creation cost | — | `IServerConfigGrain` |

> `AGENTS.md` cites `Vortex:FriendList:UserFriendLimit` as the canonical configured-limit example.
> **That key does not exist** — the limits moved to `IServerConfigGrain`.
> → [Configuration](../01-runtime/configuration.md)

## Known unknowns

- **Unverified:** the guild and forum grains beyond their participation in the purchase-event stream
  and the room-rights refresh. `GroupGrain`'s five partials, `GroupForumGrain` and
  `GuideDirectoryGrain`'s session logic were outlined, not read line by line.

## Sources

- `Vortex.Social/Grains/MessengerGrain.cs` + `.Friends.cs`, `.Messaging.cs`, `.Presence.cs`
- `Vortex.Social/Grains/{GroupGrain,GroupDirectoryGrain,GroupForumGrain,GuideDirectoryGrain,PlayerQuizGrain}.cs`
- `Vortex.Social/Events/GuideDisconnectHandlers.cs`
- `Vortex.Social/Configuration/MessengerConfig.cs`
- `Vortex.Database/Entities/**` — the messenger and group tables
- `Vortex.Primitives/Rooms/Grains/IRoomSecurity.cs` — `RefreshGroupMembershipAsync`, implemented in `Vortex.Rooms/Grains/RoomGrain.Security.cs`
