# Networking and transport

## Purpose

How bytes get on and off the wire: the two listeners, framing, encryption, and the session registry.
Nothing on this page names a header id — that is [Revisions](revisions.md).

## Scope

`Vortex.Networking/**`, `Vortex.Crypto/**`, and the packet reader/writer in `Vortex.Primitives/Packets/**`.
Dispatch begins on [Packet pipeline](packet-pipeline.md).

## Two hosts, two session types

`Vortex.Networking/NetworkManager.cs` builds **two full `IHost` instances**, lazily, under separate
`Lock` gates, and stops both with a 5 s timeout. Its only caller is `VortexEmulator.StartAsync`.

| | TCP | WebSocket |
|---|---|---|
| Builder | `SuperSocketHostBuilder.Create<IClientPacket>()` | `WebSocketHostBuilder.Create()` |
| Config section | `serverOptions:TcpServer` | `serverOptions:WebSocketServer` |
| Session type | `SessionContext : AppSession, ISessionContext` | `WebSocketSessionContext` — a plain wrapper, **not** a SuperSocket session |
| Registered by | `builder.UseSession<SessionContext>()` | constructed by hand in `UseSessionHandler`, tracked in `NetworkManager._wsSessions` |
| Port (prod / dev) | 30000 / 40000 | 30001 / 40001 |

> **Configuration trap.** These are child hosts with their own containers. `Program.cs` calls
> `AddEnvironmentVariables(prefix: "VORTEX__")`, and that prefix **never reaches them**. The
> SuperSocket hosts read *unprefixed* variables: `serverOptions__TcpServer__listeners__0__port`.
> `docker-compose.yml` documents the consequence by setting both styles.

`OrleansHostConfigValidator.GameListenerPorts()` reads the same `serverOptions` tree and refuses
startup if a listener port collides with the Orleans silo or gateway port. Pinned by
`Vortex.Hosting.Tests/ConfigValidationTests.cs` — `AGatewayPortOnTheGameListener_IsRefused`,
`ASiloPortOnTheGameListener_IsRefused`, `TheShippedPortLayout_Passes`.

## Framing

```
┌──────────────┬────────────┬──────────────────┐
│ length int32 │ header i16 │ body             │
│  big-endian  │ big-endian │                  │
└──────────────┴────────────┴──────────────────┘
   length counts the header + body, not itself
```

Inbound: `Vortex.Networking/Package/ClientPacketDecoder.cs` — `TryRead`. Needs ≥ 4 bytes, reads the
length with `BinaryPrimitives.ReadInt32BigEndian`, waits for `length + 4`, slices, then re-reads the
length and pops the header. Returns `null` for an incomplete frame **without consuming anything**.

Outbound mirror: `Vortex.Primitives/Packets/AbstractSerializer.cs` — `Serialize` writes
`WriteInteger(0)` as a placeholder, then the header, then the body, then rewinds to offset 0 and
writes `length - 4`.

Scalars are big-endian throughout. A string is a **`ushort` byte count** (not a character count)
followed by UTF-8. A boolean is one byte.

Reader/writer: `Vortex.Primitives/Packets/ClientPacket.cs` (`PopByte`/`PopShort`/`PopInt`/`PopLong`/
`PopString`/`PopBoolean`, plus `Position`, `Remaining`, `ToHexDump`) and `ServerPacket.cs`
(`WriteByte`/`WriteShort`/`WriteInteger`/`WriteLong`/`WriteString`/`WriteBoolean`/`WriteFloat`/
`WriteDouble`, `SetWriterPosition`).

Pinned by `Vortex.Rooms.Tests/Players/BenchmarkPacketFramingTests.cs`.

Bound: a declared body length below zero or above `Vortex:Networking:MaxPacketBodyBytes`
(default 65536) throws `InvalidDataException`.

## Encryption

RC4, applied **per frame**, with one subtlety that matters:

```csharp
// ClientPacketDecoder.TryRead
ctx.CryptoIn.Peek(hdr.ToArray());     // length prefix — does NOT advance the keystream
…
ctx.CryptoIn.Process(unread);         // the whole length+4 bytes
```

`Peek` is non-advancing by contract (`Vortex.Primitives/Crypto/IRc4Engine.cs`, implemented in
`Vortex.Crypto/Rc4Engine.cs`). Without it, a partial frame would consume keystream and desynchronise
every subsequent packet.

Keys are installed during the Diffie-Hellman handshake:
`Vortex.PacketHandlers/Handshake/CompleteDiffieHandshakeMessageHandler.cs` →
`MessageContext.SetupEncryption` → `ISessionContext.SetupEncryption`. `CryptoOut` is installed only
when `Vortex:Crypto:EnableServerToClientEncryption` is set.

**Encryption is optional.** A session that never handshakes leaves `CryptoIn` null and is treated as
plaintext — which is exactly how `Vortex.LoadGen/SyntheticClient.cs` drives the server on a raw
socket. RC4 is consequently the one layer the load generator does not exercise.

Outbound: `Vortex.Networking/Package/PackageEncoder.cs` — `Encode` serializes first, then applies
`pack.Session.CryptoOut`.

## The session registry

`Vortex.Networking/Session/SessionGateway.cs` holds five silo-local `ConcurrentDictionary`s:

| Map | Key → value |
|---|---|
| `_sessions` | `SessionKey` → `ISessionContext` |
| `_sessionObservers` | `SessionKey` → `ObserverEntry` |
| `_sessionToPlayer` | `SessionKey` → player id |
| `_playerToSession` | player id → `SessionKey` |
| `_playerConnectedAt` | player id → timestamp |

`SessionKey` is a `readonly record struct` over the SuperSocket session id
(`Vortex.Primitives/Networking/SessionKey.cs`).

The two mapping dictionaries are guarded by `_mappingGate`, a `SemaphoreSlim`. That is **not** a
violation of the "no manual locks" rule — that rule is about grains, and this is a service. Grain
calls are deliberately made *after* the gate is released.

### One session per player, enforced

`AddSessionToPlayerAsync` takes the gate, evicts any other player bound to this session key, **closes
any other session bound to this player** (`closeTransport: true`), writes all three maps, releases,
and only then calls `IPlayerPresenceGrain.RegisterSessionObserverAsync`.

A second login therefore kicks the first. See [Presence routing](../03-orleans/presence-routing.md)
for what that means downstream.

### The presence → socket handle

The link from a grain back to a socket is an **Orleans object reference**, not a grain:

```csharp
// SessionGateway.AddSessionAsync
var impl = new SessionContextObserver(key, this);
var observer = _grainFactory.CreateObjectReference<ISessionContextObserver>(impl);
```

`Vortex.Networking/Session/SessionContextObserver.cs` re-resolves
`_sessionGateway.GetSession(_sessionKey)` on **every** call — it is a late-binding handle, never a
captured socket. Released in `RemoveSessionAsync` via `DeleteObjectReference`.

Contract: `Vortex.Primitives/Orleans/Observers/ISessionContextObserver.cs`.

## Sending

Both transports serialize their sends with a per-session `SemaphoreSlim(1,1)`.

- **TCP** — `SessionContext.SendComposerAsync` hands `OutgoingPackage(this, composer)` to
  `Connection.SendAsync(_packageEncoder, …)`; SuperSocket calls `PackageEncoder.Encode` on its own
  writer.
- **WebSocket** — `WebSocketSessionContext.SendComposerAsync` encodes into a local
  `ArrayBufferWriter<byte>` first, then `session.SendAsync(payload)`, because the WebSocket session
  has no encoder pipeline.

`PackageEncoder.Encode` reads `pack.Session.RevisionId`, so **outbound serialization is per-session**.

## WebSocket reassembly and heartbeat

`Vortex.Networking/Ws/WsPackageHandler.cs` — `ProcessPackageAsync` calls `ctx.Touch()` **before
decoding** (a frame the server cannot parse still proves the peer is alive), appends every segment to
`ctx.WsBuffer` (an `ArrayBufferWriter<byte>` on the session context), then loops `TryRead` over the
accumulated buffer, re-packing the unconsumed tail after each frame.

That buffer is shared and unsynchronised, which is only correct if one session's packages are handled
one at a time — see *Known unknowns*.

Heartbeat: `NetworkManager.RunWsHeartbeatAsync` sends a ping after `IdleOkActivityWindow` of silence
and closes only if `PongTimeout > 0`. **It defaults to `TimeSpan.Zero` — never close** — with a
measured rationale in `Vortex.Networking/Configuration/NetworkingConfig.cs`: a frozen Chrome
background tab cannot answer a ping and is not dead.

## Failure modes

| Condition | Behaviour |
|---|---|
| Incomplete frame | `TryRead` returns null, consumes nothing, RC4 untouched |
| Body length out of range | `InvalidDataException` from the filter |
| Unknown revision id for the session | `PackageHandler` throws |
| Unmapped header id | `LogWarning "Incoming Unknown {Header}"`, dropped — **no metric** |
| Parser throws | caught in `PackageHandler.HandleCoreAsync`, logs a hex dump **and the read position**, records to `IErrorGroupingSink`, session survives |
| Composer with no serializer | `Encode` returns 0 bytes, warns, `PacketDropped("serializer_not_found")` — silent to the client |
| Rate limit exceeded | `PacketDropped("rate_limited")`, handler never runs |

The deliberate asymmetry: a *parser* failure must not kill a session (one malformed packet is not a
reason to disconnect a player), while a *framing* failure is fatal because the stream position is no
longer known.

## Configuration

| Key | Consumer | Default |
|---|---|---|
| `serverOptions:TcpServer:listeners:*` | SuperSocket TCP host | 30000 prod / 40000 dev, `127.0.0.1` |
| `serverOptions:WebSocketServer:listeners:*` | SuperSocket WS host | 30001 / 40001 |
| `Vortex:Networking:MaxPacketBodyBytes` | `ClientPacketDecoder` | 65536 |
| `Vortex:Networking:PingIntervalMilliseconds` | WS heartbeat | 10000 |
| `Vortex:Networking:IdleOkActivityWindow` | WS heartbeat | 30 s |
| `Vortex:Networking:PongTimeout` | WS heartbeat | `Zero` = never close |
| `Vortex:Networking:RateLimit:MaxPacketsPerSecond` | `TokenBucketRateLimiter` | 50 |
| `Vortex:Networking:RateLimit:BurstSize` | same | 100 (floored at the sustained rate) |
| `Vortex:Crypto:EnableServerToClientEncryption` | handshake handler | installs `CryptoOut` |

## Dead code on this path

`ISessionContext.LastActivityUtc` is **dead for TCP**. `Touch()` is called from exactly three places,
all WebSocket or `UsePingPong`. `CreateTcpSocket` never calls `UsePingPong`, and
`SuperSocketHostBuilderExtensions.RunHeartbeatAsync` is `return Task.CompletedTask;` over a
commented-out body. `UsePingPong` would also throw if used — it resolves `NetworkingConfig` as a bare
type while only `IOptions<NetworkingConfig>` is registered.

## Tests

- `Vortex.Rooms.Tests/Players/BenchmarkPacketFramingTests.cs` — the framing contract
- `Vortex.Hosting.Tests/ConfigValidationTests.cs` — listener/Orleans port collision
- `Vortex.Hosting.Tests/ListenerSecurityTests.cs` — cleartext-off-box refusal

**Not covered by any test:** `SessionGateway` (only a fake exists), `ClientPacketDecoder` directly,
`PackageHandler`, `TokenBucketRateLimiter`, and the WS reassembly loop.

## Known unknowns

- **Unknown:** whether SuperSocket handles one session's packages serially or concurrently.
  - Inspected: `UsePackageHandlingScheduler<>` is never called (zero hits repo-wide), so the
    framework default applies. SuperSocket 2.1.0 ships both `SerialPackageHandlingScheduler<T>` and
    `ConcurrentPackageHandlingScheduler<T>`.
  - Why unresolved: the framework body was not decompiled.
  - Evidence pointing at *serial*: `WsPackageHandler` mutates the shared unsynchronised `ctx.WsBuffer`
    with no lock, which is only correct under serial handling.
  - What would resolve it: decompiling `SuperSocketService<T>.HandleSession`.
- **Unknown:** whether SuperSocket closes the TCP connection when the filter throws
  `InvalidDataException`. The WS path catches and closes explicitly; the TCP path relies on the
  framework's protocol-error handling, which was not traced.
- **Unknown:** the ordering assumption that `AppSession.OnSessionConnectedAsync` runs before the
  `SessionHandlers.Connected` delegate. The gateway registration reads `ctx.SessionKey`, which is only
  set in the former, so the current code depends on it.

## Sources

- `Vortex.Networking/NetworkManager.cs` — `CreateTcpSocket`, `CreateWsSocket`, `RunWsHeartbeatAsync`
- `Vortex.Networking/Package/ClientPacketDecoder.cs`, `PackageEncoder.cs`, `PackageHandler.cs`
- `Vortex.Networking/Session/SessionGateway.cs`, `SessionContext.cs`, `SessionContextObserver.cs`
- `Vortex.Networking/Ws/WsPackageHandler.cs`, `WebSocketSessionContext.cs`
- `Vortex.Networking/Tcp/TcpFilter.cs`
- `Vortex.Networking/Extensions/SuperSocketHostBuilderExtensions.cs`
- `Vortex.Networking/Configuration/NetworkingConfig.cs`
- `Vortex.Primitives/Packets/{ClientPacket,ServerPacket,AbstractSerializer}.cs`
- `Vortex.Primitives/Networking/{ISessionContext,ISessionGateway,SessionKey}.cs`
- `Vortex.Crypto/Rc4Engine.cs`
- `Vortex.Main/Configuration/OrleansHostConfigValidator.cs`
