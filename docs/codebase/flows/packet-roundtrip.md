# Flow: packet round trip

The canonical trace — bytes in, bytes out — with the file and symbol for each hop. Every other flow
page assumes this one.

## Trigger

A framed packet arrives on the game TCP listener from an established session.

## The trace

```mermaid
sequenceDiagram
    autonumber
    participant SK as socket
    participant F as TcpFilter / ClientPacketDecoder
    participant PH as PackageHandler
    participant REV as IRevision
    participant MS as MessageSystem
    participant EH as EnvelopeHost
    participant H as IMessageHandler&lt;T&gt;
    participant G as grain
    participant PE as PackageEncoder

    SK->>F: bytes
    F->>F: peek length through RC4, wait for length+4, decrypt, pop header
    F->>PH: IClientPacket
    PH->>REV: Parsers[headerId]
    REV-->>PH: IMessageEvent
    PH->>MS: PublishAsync(message, ctx, ct)
    MS->>MS: BeginScope(correlation id) · PacketReceived
    MS->>EH: registry.PublishAsync
    EH->>EH: RateLimitBehavior [Order(int.MinValue)]
    EH->>H: fresh instance per packet
    H->>G: grain call — the authoritative mutation
    G-->>H: snapshot
    H->>PE: ctx.SendComposerAsync(composer)
    PE->>REV: Serializers[composer.GetType()]
    PE->>SK: length + header + body, RC4 out
```

## Step by step

### 1–2 · Frame and decrypt

`Vortex.Networking/Tcp/TcpFilter.cs` — `Filter` copies the `SequenceReader`, calls
`_decoder.TryRead(ref r, ctx)`, and **only commits `reader = r`** when a whole frame was produced.

`ClientPacketDecoder.TryRead`: needs ≥ 4 bytes, **peeks** the length through RC4 (non-advancing, so a
partial frame does not consume keystream), waits for `length + 4`, decrypts the whole frame, re-reads
the length, pops the 2-byte header.

Bounds: a declared body length `< 0` or `> Vortex:Networking:MaxPacketBodyBytes` (65536) throws
`InvalidDataException`.

→ [Networking](../02-network-protocol/networking.md)

### 3–4 · Header id → typed message

`Vortex.Networking/Package/PackageHandler.cs` — `HandleCoreAsync`:

```csharp
IRevision revision = _revisionManager.GetRevision(ctx.RevisionId);   // throws if unknown
if (!revision.Parsers.TryGetValue(packet.Header, out IParser? parser))
{
    _logger.LogWarning("Incoming Unknown {Header}", packet.Header);  // dropped, no metric
    return;
}
IMessageEvent message = parser.Parse(packet);
```

The session's revision was pinned by `ClientHelloMessageHandler` from the client's build string.
→ [Revisions](../02-network-protocol/revisions.md)

A parser exception is caught here, logged with a **hex dump and the read position**, and recorded to
`IErrorGroupingSink`. The session survives — one malformed packet is not a reason to disconnect.

### 5–6 · Trace and context

`Vortex.Messages/MessageSystem.cs` — `PublishAsync` resolves the actor from
`_sessionGateway.GetPlayerId(sessionKey)`, resolves the active room via
`IPlayerPresenceGrain.GetActiveRoomAsync`, opens `_contextAccessor.BeginScope(...)`, records
`PacketReceived`, and times the dispatch with `Stopwatch.GetTimestamp` → `PacketCompleted`.

`MessageRegistry.CreateContextAsync` throws `VortexException(InvalidSession)` on a null session,
otherwise builds `MessageContext(session, playerId, roomId)` — **reusing** the room id the trace scope
just resolved rather than making a second grain call (`PERF-01`).

> That presence-grain call happens on **every** inbound packet, and it runs *before* the rate limiter
> can reject one. → [Performance](../10-operations/performance.md)

### 7 · Rate limit

`RateLimitBehavior` is `[Order(int.MinValue)]` and registered against `IMessageEvent` itself, so
inheritance dispatch applies it to every concrete message type. A refusal records
`PacketDropped("rate_limited")` and never calls `next()`.

Token bucket: 50/s sustained, burst 100.

### 8 · Handler

`EnvelopeHost.BuildPipeline` runs behaviours outermost-first, then `InvokeOneAsync` calls
`h.Activator(sp)` — **a fresh handler instance per packet**, disposed afterwards if disposable.

`sp` is the **root** provider; no scope is created. That is the mechanical reason a handler cannot
hold a `DbContext`.

A handler exception is caught and routed to `OnHandlerInvokeError` — which is why `PacketFailed` is
effectively unreachable.

→ [Packet pipeline](../02-network-protocol/packet-pipeline.md)

### 9 · The authoritative mutation

The handler calls a grain through a `GrainFactoryExtensions` accessor. `ctx.PlayerId` and `ctx.RoomId`
come from the **authenticated session**, never from the packet body — that is the trust boundary.

### 10–11 · Out

Three possible paths; the choice is the architectural decision on this flow.

| Target | Call | Notes |
|---|---|---|
| **the requester** | `ctx.SendComposerAsync` | 306 sites. `MessageContext` → `ISessionContext` → per-session semaphore → socket |
| **another player** | `IPlayerPresenceGrain.SendComposerAsync` | 38 handler files. Queued (cap 500, drop oldest), drained through the observer |
| **the room** | `RoomGrain.SendComposerToRoomAsync` | Orleans memory stream → every occupant's presence grain |

→ [Presence routing](../03-orleans/presence-routing.md)

`PackageEncoder.Encode` looks the serializer up by the composer's **exact CLR type** for
`pack.Session.RevisionId`, applies `CryptoOut`, and writes. A miss returns **zero bytes**, warns, and
records `PacketDropped("serializer_not_found")` — silent to the client.

`AbstractSerializer<T>.Serialize` writes a length placeholder, the header, the body, then rewinds and
writes `length - 4`.

## Failure table

| Condition | Result | Session survives |
|---|---|---|
| Incomplete frame | nothing consumed, RC4 untouched | ✅ |
| Body length out of range | `InvalidDataException` from the filter | ❌ framing is unrecoverable |
| Unknown revision id | `PackageHandler` throws | ❌ |
| Unmapped header id | warning, dropped, **no metric** | ✅ |
| Parser throws | hex dump + error sink | ✅ |
| Rate limited | `PacketDropped("rate_limited")` | ✅ |
| Handler throws | `OnHandlerInvokeError`, other handlers still run | ✅ |
| No serializer for the composer | 0 bytes, `PacketDropped("serializer_not_found")` | ✅ but the feature is silently dead |

## What this flow does not cover

- **Authentication** — the SSO branch is [Authentication](authentication.md)
- **Room entry** — the composer burst is [Room entry](room-entry.md)
- **Money** — [Catalog purchase](catalog-purchase.md)

## Sources

- `Vortex.Networking/Tcp/TcpFilter.cs`, `Package/{ClientPacketDecoder,PackageHandler,PackageEncoder}.cs`
- `Vortex.Messages/MessageSystem.cs`, `Registry/{MessageRegistry,MessageContext}.cs`
- `Vortex.Messages/Behaviors/RateLimitBehavior.cs`
- `Vortex.Pipeline/EnvelopeHost.cs`
- `Vortex.Primitives/Packets/AbstractSerializer.cs`
- `Vortex.Primitives/Orleans/GrainFactoryExtensions.cs`
- `docs/walkthroughs/request-lifecycle.md` — the same flow for chat, though its `Vortex.Primitives/Messages` paths are stale
