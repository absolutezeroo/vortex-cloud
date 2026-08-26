# Flow: connection to authenticated player

From `accept()` to a player who can receive composers, and back out on disconnect. 23 steps, each
traceable.

## Trigger

A TCP connection on the game listener. (The WebSocket path differs only at steps 1–4 and 18.)

## Connect

**1 · Accept.** SuperSocket's TCP host accepts and constructs a `SessionContext`
(`builder.UseSession<SessionContext>()`).

**2 · Session key.** `SessionContext.OnSessionConnectedAsync` sets `SessionKey = this.SessionID`.

**3 · Gateway registration.** The `SessionHandlers.Connected` delegate installed by
`UseSessionGateway()` calls `ctx.SetRevisionId(revisionManager.DefaultRevisionId)` then
`gateway.AddSessionAsync(ctx.SessionKey, ctx)` — which puts the context in `_sessions` and mints the
`SessionContextObserver` plus its Orleans object reference.

> Ordering assumption: SuperSocket fires `OnSessionConnectedAsync` before the `Connected` delegate.
> The registration reads `ctx.SessionKey`, which is only set in the former. **Not verified from
> framework source.**

**4 · Framing.** → [Packet round trip](packet-roundtrip.md)

## Handshake — optional

**5.** `ClientHello` → `ClientHelloMessageHandler` pins the session's revision from
`message.Production`, or closes the session if it is null.

> The numeric ids on this page (`4000`, `882`) are the values **in `Revision20260701`** — they are not
> protocol constants. → [Revisions](../02-network-protocol/revisions.md)

`InitDiffieHandshake` → the server's prime and generator.
`CompleteDiffieHandshake` → `ctx.SetupEncryption(sharedKey, EnableServerToClientEncryption)`.

From the next frame on, `ClientPacketDecoder` peeks and decrypts.

> **Encryption is optional.** A session that never handshakes leaves `CryptoIn` null and is treated as
> plaintext — which is how `Vortex.LoadGen` drives the server on a raw socket.

## SSO

**6–8.** `SSOTicket` (header `882` in this revision) → `SSOTicketMessage { SSO, ElapsedMilliseconds }` →
`Vortex.PacketHandlers/Handshake/SSOTicketMessageHandler.cs` calls
`IAuthenticationService.GetPlayerIdFromTicketAsync(ticket, ctx.RemoteIpAddress, ct)`.

**9 · Ticket validation.** `Vortex.Authentication/AuthenticationService.cs`:

```
open a VortexDbContext from the factory
look up SecurityTickets by exact Ticket
expiry = ExpiresAt ?? CreatedAt + TicketTtlSeconds
  ├─ expired and not locked  → delete
  ├─ TicketSingleUse          → consume
  └─ otherwise                → slide the expiry, capped by TicketAbsoluteLifetimeSeconds
IsLocked tickets are never touched
```

Publishes `PlayerLoggedInEvent(playerId, HashIp(remoteIp))` or `PlayerLoginFailedEvent(HashIp(...))`.

> **The IP is HMAC-SHA256'd** with `Vortex:Authentication:IpHashSecret` and **never stored raw**.
> `AuthenticationConfigValidator` refuses a placeholder secret outside Development.
> `AuthenticationServiceTests.RemoteIp_IsHashed_NotStoredOrPublishedInTheClear` pins it.

`security_tickets` is unique on **both** `player_id` and `ticket` — one live SSO handoff per player,
and no ticket collision.

**10 · Gates.**

| Condition | Result |
|---|---|
| `playerId <= 0` | `ctx.CloseSessionAsync()` |
| active ban (`IPlayerGrain.GetActiveBanExpiryAsync`) | `UserBannedMessageComposer`, then close |

## Attach

**11 · `SessionGateway.AddSessionToPlayerAsync`.** Under `_mappingGate`:

1. evict any other player bound to this session key
2. **close any other session bound to this player** (`closeTransport: true`)
3. write `_sessionToPlayer`, `_playerToSession`, `_playerConnectedAt`

Then, **after releasing the gate**: `playerPresence.RegisterSessionObserverAsync(observer)` and
`IEventPublisher.PublishAsync(new PlayerConnectedEvent(...))`.

Releasing before the grain calls is deliberate — a grain call under a lock is how a service deadlocks
against a grain that calls back.

**12 · Presence binding.** `PlayerPresenceGrain.RegisterSessionObserverAsync` stores the observer in a
single nullable field. The grain key is the **player id** as an Orleans integer key.

> **A player has exactly one session.** → [Presence routing](../03-orleans/presence-routing.md)

## Login burst

**13.** `SSOTicketMessageHandler` is 664 lines — the largest handler in the repo, and legitimately so.
It issues **~15 independent grain reads**, `Task.WhenAll`s them, then emits the login composer
sequence (`AuthenticationOKMessage` … `PerkAllowancesMessageComposer`) through `ctx.SendComposerAsync`
— the reply-to-caller path.

One write (`MarkLoggedInAsync`) is hoisted **ahead** of the batch.

> The comment cites the login benchmark. Login was once 15 *sequential* grain reads, which meant
> arrivals froze the hotel while steady state looked fine.
> → [Performance](../10-operations/performance.md)

**14 · Steady state.** Every subsequent packet repeats the round trip with `ctx.PlayerId` populated.

## Disconnect

**15 · Close.** The socket closes → SuperSocket raises `Closed` → `SessionHandlers.Closed` →
`gateway.RemoveSessionAsync(ctx.SessionKey, CancellationToken.None)`.

**16 · Detach.** If the session was mapped, take `_mappingGate` and call
`RemovePlayerSessionCoreAsync(playerId, key, closeTransport: false, ct)` — which removes both mapping
halves **atomically by pair** (`TryRemovePair`) and calls
`IPlayerPresenceGrain.UnregisterSessionObserverAsync`.

**17 · Room leave.** `UnregisterSessionObserverAsync` calls `ClearActiveRoomAsync` **first**, then
nulls the observer:

```csharp
try { await roomAvatars.RemoveAvatarFromPlayerAsync(…); }
catch (Exception ex) { _logger.LogWarning(ex, …); }   // deliberate — a ghost occupant blocks re-entry
await directory.RemovePlayerFromRoomAsync(…, CancellationToken.None);
// unsubscribe the room stream
```

**18 · Lifecycle event.** `PlayerDisconnectedEvent(playerId, connectedAt, disconnectedAt, durationSeconds)`.
Consumers:

| Handler | Does |
|---|---|
| `Vortex.Social/Events/GuideDisconnectHandlers.cs` | clears duty, ends the guide session, notifies the partner with `EndReason = 0` |
| `Vortex.Players/Events/ModerationQueueDisconnectHandler.cs` | unsubscribes from the CFH queue |
| `Vortex.Observability/Events/SessionLifecycleAuditHandlers.cs` | writes the audit row |

**19 · Teardown.** `DeleteObjectReference<ISessionContextObserver>` (failure logged at Debug), then
`_sessions.TryRemove(key)`.

> **Disconnect triggers no persistence flush of its own.** Room state flushes on
> `RoomPersistenceGrain`'s own timer; audit rows come from the event handlers.
> → [State ownership](../03-orleans/persistence.md)

## Configuration

| Key | Effect | Default |
|---|---|---|
| `Vortex:Authentication:IpHashSecret` | HMAC key; empty ⇒ null hash; placeholder refused outside Development | — |
| `Vortex:Authentication:TicketTtlSeconds` | fallback expiry when `ExpiresAt` is null; `0` disables | 30 |
| `Vortex:Authentication:TicketSingleUse` | delete on first use | **false** |
| `Vortex:Authentication:TicketAbsoluteLifetimeSeconds` | caps the sliding refresh; ignored when single-use | — |
| `Vortex:Crypto:EnableServerToClientEncryption` | installs `CryptoOut` | — |

## Where tickets come from

`Vortex.WebApi`'s `/api/ssotoken` issues them, after a login that shares `IAccountAuthenticator` with
the dashboard — so the second factor cannot fall behind on one of them. Wall 5 of
`check-architecture-walls.mjs` forbids a second `BCrypt.Verify` outside `Vortex.Authentication`.

→ [Hosting](../01-runtime/hosting.md)

## Tests

`Vortex.Authentication.Tests/AuthenticationServiceTests.cs` — 13 facts covering expiry, locked
tickets, sliding refresh, the null-`ExpiresAt` TTL fallback, `TicketTtlSeconds = 0`, and IP hashing.

**Not covered:** `SessionGateway` has no unit tests — only a fake exists.

## Sources

- `Vortex.Networking/Session/{SessionContext,SessionGateway,SessionContextObserver}.cs`
- `Vortex.Networking/Extensions/SuperSocketHostBuilderExtensions.cs`
- `Vortex.PacketHandlers/Handshake/{ClientHelloMessageHandler,InitDiffieHandshakeMessageHandler,CompleteDiffieHandshakeMessageHandler,SSOTicketMessageHandler}.cs`
- `Vortex.Authentication/AuthenticationService.cs`, `Configuration/AuthenticationConfig.cs`
- `Vortex.Players/Grains/PlayerPresenceGrain.cs`, `.Room.cs`
- `Vortex.Database/Entities/Security/SecurityTicketEntity.cs`
- `Vortex.Authentication.Tests/AuthenticationServiceTests.cs`
