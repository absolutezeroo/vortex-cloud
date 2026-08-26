# Packet pipeline

## Purpose

How a decoded frame reaches one of 539 handlers, how those handlers got registered without a single
DI entry or attribute, and what the handler contract actually permits.

## Three lookups, three key types

```
bytes ──► int header id ──► IParser         (revision table)
                              │
                              ▼
                        IMessageEvent
                              │
      System.Type of the message ──► behaviours + handler bucket   (EnvelopeHost)
                              │
                              ▼
                          IComposer
                              │
      System.Type of the composer ──► ISerializer   (revision table)
                              │
                              ▼
                            bytes
```

Only the middle one is inheritance-aware.

## The envelope host

`Vortex.Pipeline/EnvelopeHost.cs` is a generic behaviour/handler pipeline. **Two systems are the same
machine**, differently closed:

| | Packet pipeline | Event pipeline |
|---|---|---|
| Envelope | `IMessageEvent` | `IEvent` |
| Registry | `Vortex.Messages/Registry/MessageRegistry.cs` | `Vortex.Events/Registry/EventRegistry.cs` |
| Context | `MessageContext(session, playerId, roomId)` | `EventContext` — `Cancel`, `CancelReason`, `CorrelationId`, `Items` |
| Handler | `IMessageHandler<T>` | `IEventHandler<T>` |
| Short-circuit | none | `ctx => ctx.Cancel` — the event bus is **cancellable** |
| Missing handler | logs a warning | silent |

Both use `HandlerMode = Parallel`, `MaxHandlerDegreeOfParallelism = null`, and
`EnableInheritanceDispatch = true` (dispatch walks base types *and* interfaces via
`EnvelopeHost.EnumerateTypeGraph`). Behaviour order comes from `[Order]`.

Do not conflate them: the event bus is in-process domain notification, not the wire. See
[Plugins](../09-extensibility/plugins.md) for the event side.

`_byEvent` is a `ConcurrentDictionary<Type, Bucket<TContext>>`; pipelines are built once per bucket
version and cached.

## Registration: a reflection scan, nothing else

There is **no attribute, no DI registration and no explicit map** for handlers.

```
PluginBootstrapper.StartAsync
  └─ for each registered IHostPluginModule (15 of them, in parallel)
       └─ AssemblyProcessor.ProcessAsync(module.GetType().Assembly, sp, ct)
            └─ every IAssemblyFeatureProcessor
                 └─ MessageFeatureProcessor
                      └─ EnvelopeFeatureProcessor.ProcessAsync
                           ├─ AssemblyExplorer.FindClosedImplementations(asm, typeof(IMessageHandler<>))
                           ├─ args[0] is the envelope type
                           ├─ EnvelopeInvokerFactory.CreateHandlerInvoker  (expression tree)
                           ├─ ActivatorHelpers.BuildActivator
                           └─ EnvelopeHost.RegisterHandler(envType, sp, activator, invoker)
```

`Vortex.PacketHandlers/PacketHandlersModule.cs` has an **empty `ConfigureServices`**. It exists only
to bring its own assembly into that scan. That is how 539 handlers register with zero registration
code.

Every registration returns an `IDisposable` — the disposable *is* the deregistration, which is what
makes plugin unload clean.

There are exactly **four** `IAssemblyFeatureProcessor` implementations, and they are the entire
extension surface a scanned assembly offers:

| Processor | Scans for |
|---|---|
| `MessageFeatureProcessor` | `IMessageHandler<>` / `IMessageBehavior<>` |
| `EventFeatureProcessor` | `IEventHandler<>` / `IEventBehavior<>` |
| `RoomObjectLogicFeatureProcessor` | `[RoomObjectLogic]` furniture logic classes |
| `WiredVariableFeatureProcessor` | wired variables |

**Nothing scans for `IRevision`** — see [Protocol revision plugins](../09-extensibility/protocol-revision-plugins.md).

> **Trap:** `AssemblyExplorer.FindClosedImplementations` skips anything not `public`, and also skips
> abstract and generic types. A `internal` or nested handler compiles, ships, and is **silently never
> registered**.

One registration does not come from a module assembly: `Vortex.Messages/MessageBehaviorRegistrationService.cs`
is an `IHostedService` that scans `Assembly.GetExecutingAssembly()` because `Vortex.Messages` is not a
plugin module. It exists for exactly one type — `RateLimitBehavior`.

## Per-packet flow

1. `PackageHandler.HandleCoreAsync` resolves the session's `IRevision` and parses.
2. `MessageSystem.PublishAsync` (`Vortex.Messages/MessageSystem.cs`) resolves `actorId` from
   `_sessionGateway.GetPlayerId(sessionKey)`, resolves the active room via
   `IPlayerPresenceGrain.GetActiveRoomAsync`, opens `_contextAccessor.BeginScope(...)`, records
   `PacketReceived`, and times the dispatch into `PacketCompleted`.
3. `MessageRegistry.CreateContextAsync` throws `VortexException(InvalidSession)` on a null session,
   otherwise builds `MessageContext`, **reusing** the room id the trace scope just resolved rather
   than making a second grain call (the comment cites `PERF-01`).
4. `RateLimitBehavior` (`[Order(int.MinValue)]`, registered against `IMessageEvent` itself so
   inheritance dispatch reaches every concrete message) calls `IRateLimiter.TryConsume(sessionKey)`.
   A refusal records `PacketDropped("rate_limited")` and never calls `next()`.
5. `EnvelopeHost.BuildPipeline` runs behaviours outermost-first, then invokes handlers.
6. `InvokeOneAsync` calls `h.Activator(sp)` — **a fresh handler instance per packet**, disposed
   afterwards if disposable.

> **`sp` is the root provider, not a scope.** No scope is created per packet. That is the mechanical
> reason "no DB context in a handler" holds: a scoped service could not be resolved here even if
> someone tried.

Full trace with the authentication branch: [Packet round trip](../flows/packet-roundtrip.md).

## The handler contract

A handler validates input, calls a grain or domain service, and maps the result to a composer.

```csharp
public sealed class MoveObjectMessageHandler(IRoomService roomService) : IMessageHandler<MoveObjectMessage>
{
    public async ValueTask HandleAsync(MoveObjectMessage message, MessageContext ctx, CancellationToken ct)
    {
        await _roomService.MoveFloorItemInRoomAsync(ctx.AsActionContext(), …).ConfigureAwait(false);
    }
}
```

`ctx.PlayerId` and `ctx.RoomId` come from the authenticated session — **never** from the packet body.
That is the trust boundary.

### Verified across the whole tree

> Counting note: **539 handler types across 538 files** —
> `Room/Engine/VortexFurniDefinitionMessageHandlers.cs` declares two. Both numbers appear on this
> page; the noun says which.

| Rule | Result |
|---|---|
| No DB access | **0** occurrences of `DbContext`, `IDbContextFactory`, `EntityFrameworkCore`, `SaveChanges`, `ExecuteUpdate`, `ExecuteDelete`, `FromSql` in any of 538 handler files |
| No ad-hoc grain keys | **0** occurrences of `GetGrain<` — all access via `GrainFactoryExtensions` |
| No silent catches | **0** occurrences of `catch {` or `catch (Exception)` without a bound variable |

Four handlers read in full as worked examples:

| Handler | Shape |
|---|---|
| `Room/Engine/MoveObjectMessageHandler` | pure passthrough to `IRoomService`. Textbook |
| `Help/GetMyCfhReportStatusMessageHandler` | guard → `ICfhTicketService` → reply |
| `Catalog/GetCatalogPageMessageHandler` | ~60 lines of snapshot→composer mapping, `catch (Exception ex)` **with** a log. Mapping, not domain logic — inside the contract, at its edge |
| `Handshake/SSOTicketMessageHandler` | 664 lines, the largest. ~15 grain reads deliberately batched (the comment cites the login benchmark), one write hoisted ahead of the batch. Orchestration at scale, not persistence |

### The one standing conflict

`AGENTS.md` forbids sending "directly to raw sockets/session transports from packet handlers".
`MessageContext.SendComposerAsync` forwards straight to `ISessionContext.SendComposerAsync`, which
takes a semaphore and calls `Connection.SendAsync(...)` — a direct socket write.

**222 of 538 handler files use it, across 306 call sites**, including three of the four above.

This is not a few strays. It is the de facto request/response reply path for the entire codebase,
with the presence grain as the de facto *unsolicited push* path. Both readings of the rule are
consistent with what is on disk:

- the rule means "no unsolicited fan-out from handlers", and its wording is too broad, **or**
- 222 files are in violation.

Documented, not arbitrated — it needs a decision from whoever owns the rule. Practical guidance for
the next handler is on [Architecture boundaries](../00-overview/architecture-boundaries.md).

## Where the message types live

**`Vortex.Primitives/Messages/` does not exist.** Incoming records and outgoing composers are in
`Vortex.Protocol/Messages/{Incoming,Outgoing}/<Domain>/`.

The move is commit `7919b871` "refactor: split the wire protocol out of the hub" (2026-08-23) —
1091 files renamed at R100, a pure move. The namespaces read `Vortex.Protocol.Messages.*`.

> **`AGENTS.md` is stale on this path** — 6 references, plus 13 more across `docs/glossary.md`,
> `docs/walkthroughs/add-a-feature.md` and `docs/walkthroughs/request-lifecycle.md`. The commit
> message for `7919b871` is also wrong (it claims the namespaces were kept); the csproj comment and
> the source agree that they were not.

| Concern | Type | Where |
|---|---|---|
| Incoming marker | `IMessageEvent` — empty | `Vortex.Primitives/Networking/` |
| Outgoing marker | `IComposer` — empty | `Vortex.Primitives/Networking/` |
| Incoming records | 540 types, 42 domains | `Vortex.Protocol/Messages/Incoming/` |
| Outgoing composers | 553 files | `Vortex.Protocol/Messages/Outgoing/` |

The markers stay in the hub, the records live in `Vortex.Protocol`. That split is what lets Wall 3 be
a hard zero: the hub can name `IComposer` without naming a single message.

Conventions:

```csharp
// incoming — required members, no default fallbacks
public record MoveObjectMessage : IMessageEvent
{
    public required RoomObjectId ObjectId { get; init; }
    public required int X { get; init; }
    …
}

// outgoing — Orleans-serializable, because composers cross grain boundaries
[GenerateSerializer, Immutable]
public sealed record XMessageComposer : IComposer { [Id(0)] public required … }
```

537 of 553 outgoing files carry `[GenerateSerializer]`; only 9 of 540 incoming do — incoming messages
are consumed in the handler and never travel to a grain.

**One incoming type has no handler**: `UpdateWiredMessage`, the abstract wired base. Everything else
is covered.

## Configuration

| Key | Consumer | Default |
|---|---|---|
| `Vortex:Networking:RateLimit:MaxPacketsPerSecond` | `TokenBucketRateLimiter` | 50 |
| `Vortex:Networking:RateLimit:BurstSize` | same | 100 |
| `Vortex:Protocol:*` | 5 domain maps via `ProtocolLimitsConfig` | see [Revisions](revisions.md) |

## Instrumentation gaps

Three, all worth knowing before trusting a dashboard number:

1. **`PacketFailed` is effectively unreachable.** `MessageSystem` records it only if
   `_registry.PublishAsync` throws, but `EnvelopeHost.InvokeOneAsync` catches every handler exception
   and routes it to `OnHandlerInvokeError`. Handler failures produce an error-sink record and a
   warning, never the failure counter.
2. **An unknown header id records no metric.** It logs at Warning and returns. Every *other* drop
   reason has a counter.
3. **One presence-grain round trip per packet.** `MessageSystem.ResolveRoomIdAsync` calls
   `GetActiveRoomAsync` for every inbound packet from an authenticated session, purely for the trace
   room id — and it runs *before* `RateLimitBehavior` can reject the packet. Not benchmarked here.

## Tests

`Vortex.Pipeline.Tests/EnvelopeHostTests.cs` — 14 facts: `DefaultHandlerMode_IsParallel`,
`MultipleHandlersForSameEnvelopeType_AllRun_UnderTheDefaultParallelMode`,
`SequentialMode_HandlersRunInRegistrationOrder`,
`Behaviors_RunInOrderAttributeSequence_BeforeHandlers`,
`OnHandlerInvokeError_Fires_AndOtherHandlersStillRun`,
`OnNoHandlerRegistered_FiresWhenTheBucketExistsButHasNoHandlers`,
`MaxHandlerDegreeOfParallelism_BoundsConcurrentHandlerExecutions`.

`Vortex.Hosting.Tests/ProjectBoundaryTests.cs` — the handler/DB boundary, both halves.

## Known unknowns

- **Unknown:** whether the `ctx.SendComposerAsync` pattern is intended. See *The one standing
  conflict*. Inspected: all 306 call sites, `MessageContext`, `SessionContext`, `AGENTS.md`,
  `CONTEXT.md`. Unresolved because it is a policy question, not a code question. Resolved by either a
  wording change in `AGENTS.md` or a refactor of 222 files.

## Sources

- `Vortex.Pipeline/{EnvelopeHost,EnvelopeFeatureProcessor,EnvelopeInvokerFactory,EnvelopeHostOptions}.cs`
- `Vortex.Pipeline/Attributes/OrderAttribute.cs`
- `Vortex.Messages/{MessageSystem,MessageFeatureProcessor,MessageBehaviorRegistrationService}.cs`
- `Vortex.Messages/Registry/{MessageRegistry,MessageContext}.cs`
- `Vortex.Messages/Behaviors/RateLimitBehavior.cs`
- `Vortex.Runtime/AssemblyProcessing/{AssemblyProcessor,AssemblyExplorer,IAssemblyFeatureProcessor}.cs`
- `Vortex.Plugins/PluginBootstrapper.cs`
- `Vortex.PacketHandlers/PacketHandlersModule.cs` and the handlers cited above
- `Vortex.Protocol/Vortex.Protocol.csproj` — `RootNamespace`
- [Packet handler index](../generated/packet-handler-index.md)
