# Glossary

Turbo Cloud sits at the intersection of two dense vocabularies: **Habbo domain terms**
(controller level, rights, wired, furni) and **Turbo/Orleans architecture terms**
(grain, presence, snapshot, composer, system vs module). This glossary defines each and
points to the file that *embodies* it, so a term is never abstract — you can open the
code that implements it.

Terms are grouped: Architecture, then Messaging, then Room domain, then Players, then
Furniture & Wired. Within each group, terms are ordered roughly from foundational to
specific.

---

## Architecture & Orleans

**Grain**
An Orleans virtual actor: a single-threaded, individually-addressable stateful object
that Orleans activates on demand and can deactivate, migrate, or rehydrate
transparently. In Turbo, the big ones are one room and one player per grain.
→ `Turbo.Rooms/Grains/RoomGrain.cs`, `Turbo.Players/Grains/PlayerGrain.cs`

**Silo**
An Orleans server process that hosts activated grains. A deployment can run several;
grains move between them. You rarely reference silos directly — the design's whole goal
is that domain code doesn't have to.
→ configured per `docs/orleans.md`

**Module**
A plain class that owns a slice of a grain's **state** and the operations on it. Holds a
back-reference to its grain; instantiated once at activation. Calling a module is an
in-process method call — **no** Orleans round-trip.
→ `Turbo.Rooms/Grains/Modules/` (e.g. `RoomAvatarModule`, `RoomFurniModule`,
`RoomSecurityModule`, `RoomMapModule`)

**System**
A plain class that runs **behavior/logic** over grain state (as opposed to owning a
state slice). Same back-reference pattern as a module. The split is a convention: nouns
of state → modules, engines of behavior → systems.
→ `Turbo.Rooms/Grains/Systems/` (e.g. `RoomChatSystem`, `RoomPathingSystem`,
`RoomWiredSystem`, `RoomRollerSystem`, `RoomAvatarTickSystem`)

**Partial grain**
A grain class split across many files with `partial` to keep each concern readable
(avatar, furni, map, …) without one 3,000-line file. All partials compile into one
class.
→ `Turbo.Rooms/Grains/RoomGrain.*.cs`

**Stream**
An Orleans pub/sub channel. A room publishes outgoing composers to its stream; player
presence grains subscribe. This is what decouples "something happened in the room" from
"deliver bytes to connections."
→ publish: `RoomGrain.SendComposerToRoomAsync`; subscribe:
`PlayerPresenceGrain.Room.cs`; payload: `Turbo.Primitives/Rooms/Snapshots/RoomOutbound.cs`

**Snapshot**
An immutable, serializable view of grain state passed across boundaries instead of
exposing mutable internals. Read `docs/orleans.md` → "Snapshots: When and Why."
→ `Turbo.Primitives/**/Snapshots/**` (e.g. `RoomAvatarSnapshot`, `RoomSnapshot`)

**Provider**
A service that supplies static/loaded reference data into grains (room models, item
definitions, avatar/logic factories) so grains don't load it themselves.
→ injected in `RoomGrain` ctor: `IRoomModelProvider`, `IRoomItemsProvider`,
`IRoomObjectLogicProvider`, `IRoomAvatarProvider`

**Host plugin module**
The unit of composition/registration. Each `Turbo.*` area exposes one, wiring its
services into DI at startup. Not to be confused with a *grain* module.
→ interface `IHostPluginModule`; example `Turbo.Authentication/AuthenticationModule.cs`

---

## Messaging & networking

**Incoming message**
A typed object representing a packet the client sent, after decoding. Plain data, no
logic.
→ `Turbo.Primitives/Messages/Incoming/**` (e.g. `ChatMessage`)

**Composer (outgoing message)**
The outgoing counterpart to an incoming message — a typed object that knows how to be
serialized to the client. Implements `IComposer`.
→ `Turbo.Primitives/Messages/Outgoing/**` (e.g. `ChatMessageComposer`)

**Handler**
The class that receives one decoded incoming message type and orchestrates the
response. **Orchestration only**: validate context, resolve a grain, delegate. No DB, no
socket, no business logic.
→ `Turbo.PacketHandlers/**`; shape in `docs/patterns/HandlerPattern.cs`

**MessageContext**
The ambient per-request data handed to a handler: `PlayerId`, `RoomId`, and
`AsActionContext()`. The **trust boundary** — ids come from the authenticated session,
not the packet.
→ used in every handler under `Turbo.PacketHandlers/**`

**ActionContext**
A context for room *actions* (movement, furniture use) carrying the actor plus an
**`Origin`** (`Player`, `System`, …). `Origin` lets the room treat system-driven actions
differently from player-driven ones.
→ `Turbo.Primitives/Action/**`; produced by `MessageContext.AsActionContext()`

**Pipeline**
The middleware that routes a decoded message to its handler and runs registered
behaviors around it (ordering, cross-cutting concerns).
→ `Turbo.Pipeline/` (`EnvelopeHost`, `Registry/IHandler`, `Registry/IBehavior`,
`Registry/Bucket`)

**Revision**
Everything specific to one client build's wire format — which header maps to which
message, and how composers serialize. **Owned by the plugin repo, not `turbo-cloud`.**
→ `../turbo-sample-plugin/TurboSamplePlugin/Revision/**` (per `CONTEXT.md`);
selection logic `Turbo.Networking/Revisions/RevisionManager.cs`

**Session**
A live client connection. Holds the socket and serializes composers for that client's
revision. Domain code never touches it directly — presence grains do.
→ `Turbo.Networking/Session/` (`SessionGateway`, `SessionObserver`, `SessionContext`)

---

## Room domain

**Room object**
Anything that exists *in* a room as a positioned entity: an avatar, a piece of
furniture. Addressed by a `RoomObjectId`. Chat and actions target room objects, not
players directly, because that's what other clients render.
→ `Turbo.Rooms/Object/**`; ids in `Turbo.Primitives/Rooms/Object/**`

**Controller level**
The room-local authority a player has, as an **ordinal** enum:
`None < Rights < GroupMember < GroupRights < GroupAdmin < Owner` (and `System` origin
resolves high, to `Moderator`). Most room permission checks compare against a threshold,
e.g. `>= RoomControllerType.Rights`.
→ enum `RoomControllerType`; resolution `RoomSecurityModule.GetControllerLevelAsync`

**Rights**
The classic Habbo notion of "the owner gave this user build/furni permissions in this
room." Distinct from any global staff rank. Stored per room+player.
→ `Turbo.Database/Entities/Room/RoomRightEntity.cs`; in-memory
`RoomGrain._state.PlayerIdsWithRights`

**Owner**
The player who owns the room. Top of the non-staff controller ordinal.
→ `RoomSecurityModule.GetIsRoomOwnerAsync` (checks `_state.RoomSnapshot.OwnerId`)

**Security module**
The room's permission gate: given an `ActionContext`, decides whether the actor may
manipulate/use/place/pick furniture, and computes the controller level. Currently
contains stubs (`// if has perm …`) where global staff permissions will plug in.
→ `Turbo.Rooms/Grains/Modules/RoomSecurityModule.cs`

**Map module / pathing system**
The map module owns walkability and tile indexing; the pathing system runs A\* over it
to move avatars (cardinal cost 10, diagonal 14, heuristic-guided).
→ `Turbo.Rooms/Grains/Modules/RoomMapModule.cs`,
`Turbo.Rooms/Grains/Systems/RoomPathingSystem.cs`

**Roller system**
Drives roller furniture that moves avatars/items one tile per tick.
→ `Turbo.Rooms/Grains/Systems/RoomRollerSystem.cs`

---

## Players

**Player vs Player account**
A **player** is an avatar (name, figure, motto, inventory) — the in-world identity. A
**player account** is the login that may own one or more players (Habbo's
multi-avatar-per-account shape). Note: the current public schema models `players`
directly; the account layer is a known extension point.
→ `Turbo.Database/Entities/Players/PlayerEntity.cs`

**Player grain**
Authoritative per-player state and snapshots (profile, summary) consumed by handlers.
→ `Turbo.Players/Grains/PlayerGrain.cs`

**Presence grain**
Tracks where a player currently *is* (active/pending room) and owns delivery to that
player's session(s). It subscribes to room streams and fans out composers. The bridge
between room broadcasts and sockets.
→ `Turbo.Players/Grains/PlayerPresenceGrain.cs` (+ `.Room.cs`, `.Avatar.cs`, etc.)

**Directory grain**
Owns username↔id lookup and cache coherence (case-insensitive; forward+reverse
mappings kept consistent). Handlers must not bypass it with their own lookups.
→ `Turbo.Players/Grains/PlayerDirectoryGrain.cs` (see `CONTEXT.md` boundaries)

**Perk flags**
A bitflag enum of player capabilities on the player entity. Cheap for a handful of
perks; watch it as a scaling risk (a `[Flags]` enum can't be extended by a plugin
without touching core — the kind of thing a node-based permission system would later
absorb).
→ `PlayerEntity.PlayerPerks` / `PlayerPerkFlags`

**Security ticket**
The short-lived token exchanged at login (SSO) for a player id. Consumed (deleted)
unless locked.
→ `Turbo.Database/Entities/Security/SecurityTicketEntity.cs`;
`Turbo.Authentication/AuthenticationService.cs`

---

## Furniture & Wired

**Furni / furniture**
Placeable items in a room, split into **floor** and **wall** variants throughout the
code.
→ `Turbo.Rooms/Object/Furniture/{Floor,Wall}/**`;
`Turbo.Rooms/Grains/Modules/RoomFurniModule.{Floor,Wall}.cs`

**Stuff data**
The per-item variable payload (state, colors, custom values) that travels with a piece
of furniture and is sent to clients to render its current condition.
→ `Turbo.Primitives/Furniture/StuffData/**`,
`Turbo.Primitives/Furniture/Snapshots/StuffData/**`

**Logic (furniture logic)**
The behavior attached to a furniture type — what it does when clicked/used. Selected by
a logic provider per item.
→ `Turbo.Rooms/Object/Logic/Furniture/**`; chosen via `IRoomObjectLogicProvider`

**Wired**
Habbo's in-room visual programming: **triggers** (when X happens), **conditions** (if Y
holds), **actions** (do Z), plus selectors, addons, and variables. Turbo implements all
four families as furniture logic.
→ `Turbo.Rooms/Object/Logic/Furniture/Floor/Wired/{Triggers,Conditions,Actions,Selectors,Addons,Variables}/**`;
engine `Turbo.Rooms/Grains/Systems/RoomWiredSystem.cs`

**Wired trigger / condition / action**
- *Trigger*: the event that starts a wired stack (e.g. user walks on tile).
- *Condition*: a gate that must pass for actions to run (e.g. team has rank).
- *Action*: the effect performed when a trigger fires and conditions pass.
→ e.g. `WiredTriggerClickTile`, `WiredConditionTeamHasRank` under the paths above

**Wired variable**
Stored values wired stacks read/write, scoped to furniture, room, user, or context.
→ `Turbo.Rooms/Wired/Variables/{Furniture,Room,User,Context}/**`

---

## See also

- `docs/orleans.md` — the architecture mental model these terms live inside
- `docs/walkthroughs/request-lifecycle.md` — the terms in motion, one real packet
- `docs/walkthroughs/add-a-feature.md` — where each term's code goes when you build
- `CONTEXT.md` — the hard boundaries between these concepts
