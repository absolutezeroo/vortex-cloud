# Protocol revision plugins

## Purpose

Where a revision may live, and the one mechanism by which a plugin can supply one. This page is
unusually careful about what is proven, because the sibling repository is not present on this machine.

## The rule

| Revision | Lives in | Editing it here |
|---|---|---|
| `Revision20260701` — the **embedded default** | `Vortex.Revisions/Revision20260701/**` | **correct and expected** |
| any additional/custom revision | `../turbo-sample-plugin/TurboSamplePlugin/Revision/**` | **do not** create a second `Revision<id>/Parsers` tree here |

`AGENTS.md` and `CONTEXT.md` both state this. The embedded default exists so the emulator runs
standalone without a plugin.

> **Two READMEs contradict it.** `Vortex.Revisions/README.md` and
> `Vortex.Revisions/Revision20260701/README.md` both state that "vortex-cloud does not own
> `Revision<id>/Parsers` or `Serializers`", describe the pre-Maps single-dictionary checklist, and
> name a `RevisionDefault/RevisionDefault.cs` that does not exist. They are stale; `AGENTS.md` and
> `CONTEXT.md` are right.

## How the embedded revision is registered

By contract, so nothing in the host names a concrete revision type:

```csharp
// Vortex.Revisions/Extensions/ServiceCollectionExtensions.cs
using RevisionType = Vortex.Revisions.Revision20260701.Revision20260701;
services.AddSingleton<IRevision, RevisionType>();
```

`Vortex.Revisions/RevisionRegistrationService.cs` is an `IHostedService` that enumerates
`IEnumerable<IRevision>` **from the host container** and calls `IRevisionManager.RegisterRevision` on
each, then `SetDefault(RevisionConfig.DefaultRevisionId)` when configured.

`RevisionManager.RegisterRevision` is an unconditional `Revisions[revision.Revision] = revision`
overwrite, and sets `DefaultRevisionId` if it is still empty — so with `Vortex:Revisions:DefaultRevisionId`
unset (it is), **first registration wins**.

> `docs/client-server-architecture.md` attributes revision registration to `VortexEmulator.StartAsync`.
> It does not — `VortexEmulator.cs` contains no revision code. Stale.

## The mechanism a plugin must use

Here is the part that matters, and it follows from one fact:

> **A plugin's `ConfigureServices` populates the plugin's *own* `ServiceProvider`, not the host
> container.** `RevisionRegistrationService` enumerates the *host* container. So an `IRevision`
> registered in `IVortexPlugin.ConfigureServices` is **never seen**.

Nor is there an assembly scan to fall back on — there are exactly four `IAssemblyFeatureProcessor`
implementations (messages, events, room-object logic, wired variables) and **none scans for
`IRevision`**. → [Plugins](plugins.md)

The only path this repository's code allows:

```csharp
// inside IVortexPlugin.StartAsync or BindExportsAsync
var revisions = hostServices.GetRequiredService<IRevisionManager>();
revisions.RegisterRevision(new MyRevision());
```

`Vortex.Plugins/HostServices.cs` makes that possible — it forwards to the full host provider with no
allow-list.

> **No code in this repository does this.** The mechanism is inferred from the contracts, not
> demonstrated.

## What a revision has to supply

```csharp
interface IRevision {
    string Revision { get; }                                   // the client build string
    IReadOnlyDictionary<int,  IParser>     Parsers     { get; }
    IReadOnlyDictionary<Type, ISerializer> Serializers { get; }
}
```

Deriving from `RevisionBase` gives the map-composition machinery, duplicate detection, and the
`ConfigureParsers` / `ConfigureSerializers` hooks that let a descendant revision **override individual
entries without duplicating the table**. That inheritance hook is one of the three stated reasons the
43-map split exists. → [Revisions](../02-network-protocol/revisions.md)

## Which revision a session gets

`ClientHelloMessageHandler` pins `ctx.SetRevisionId(message.Production)` — the client's own build
string. A build nobody registered leaves the session on an id no revision answers to: inbound throws,
outbound is dropped, the socket stays open and stops speaking. Deliberate, and commented as such.

So a plugin adding a revision needs its `Revision` string to match exactly what that client sends.

## Packaging

The rule from [Plugins](plugins.md) applies with force here: **do not ship `Vortex.Primitives.dll` or
`Vortex.Protocol.dll` in the plugin folder.** `ByteLoadingAlc` byte-loads any DLL it finds there,
which would give the plugin its own `IRevision` and `IComposer` types — and the host's
`IRevisionManager` would refuse them as unrelated types.

`Vortex.Plugins.Tests`' `CopyTestPluginToTempFolder` copies only the plugin DLL for exactly this
reason.

## Known unknowns

- **Unknown:** everything about how `TurboSamplePlugin` actually does this.
  - Inspected: `ls ..` returns only `vortex-emulator` and `vortex-modern-client`. **The sibling repo
    is not present on this machine.**
  - Consequence: how it declares its manifest, registers parsers and serializers, and is built or
    hot-reloaded is entirely **Unverified**. Only the receiving contracts in this repository are
    proven.
  - What would resolve it: checking out `../turbo-sample-plugin` and reading
    `TurboSamplePlugin/Revision/**`.
- **Unknown:** whether a plugin-supplied revision has ever been exercised end to end. No test in this
  repository registers an `IRevision` through `IHostServices`.
- **Doc drift:** `README.md` claims `TurboSamplePlugin` is in `Vortex.Cloud.sln`. It is not — the
  solution holds 49 projects, all `Vortex.*`.

## Sources

- `Vortex.Primitives/Networking/Revisions/{IRevision,IRevisionManager,IRevisionMap,IRevisionMapBuilder}.cs`
- `Vortex.Revisions/{RevisionBase,RevisionMapBuilder,RevisionRegistrationService}.cs`
- `Vortex.Revisions/Extensions/ServiceCollectionExtensions.cs`
- `Vortex.Revisions/Configuration/RevisionConfig.cs`
- `Vortex.Networking/Revisions/RevisionManager.cs`
- `Vortex.PacketHandlers/Handshake/ClientHelloMessageHandler.cs`
- `Vortex.Plugins/{HostServices,PluginManager}.cs`
- `Vortex.Runtime/AssemblyProcessing/ByteLoadingAlc.cs`
- `AGENTS.md`, `CONTEXT.md`, `CLAUDE.md`
