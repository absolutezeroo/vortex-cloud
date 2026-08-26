# Plugins

## Purpose

How an external assembly becomes part of a running hotel, what the load context does and does not
isolate, and why activation is transactional.

## Two different things called "plugin"

| | `IHostPluginModule` | `IVortexPlugin` |
|---|---|---|
| What | an **in-tree** domain module | an **external** assembly loaded from disk |
| Contract | `Key`, `ConfigureServices(IServiceCollection, HostApplicationBuilder)` | `Key`, `Version`, `ConfigureServices`, `StartAsync`, `StopAsync`, `BindExportsAsync`, `IAsyncDisposable` |
| Registered by | `AddHostPlugin<T>` in `Program.cs` — 15 of them | discovered at runtime by `PluginManager` |
| Container | the **host** container | its **own** `ServiceProvider` |

Both get their assembly scanned for handlers and logic. Only the second is loaded, unloaded and
reloaded.

> `IHostPlugin` (`Vortex.Primitives/Plugins/IHostPlugin.cs`) is an empty marker interface with **no
> implementors and no consumers** — a dead contract.

## Discovery

`PluginManager.DiscoverPlugins()` reads two sources, in order:

1. **`PluginConfig.DevPluginPaths`** — each entry is *itself* a plugin folder; `manifest.json` is read
   directly from it. `Path.GetFullPath`-resolved, missing directories silently skipped.
2. **`PluginConfig.PluginFolderPath`** — each *subdirectory* is a plugin folder. Default
   `AppContext.BaseDirectory/plugins`.

A `HashSet<string> seen` on `manifest.Key` means **dev paths win a key collision**.

> `README.md` says a warning is logged on collision. It is not — the duplicate is silently skipped via
> `seen.Add`.

Manifest read failures are logged as errors and the plugin is skipped, never fatal.

### The manifest

`Vortex.Primitives/Plugins/PluginManifest.cs` — `Name`, `Key`, `Version`, `Author`, `AssemblyFile`
required, plus `Dependencies`, `TablePrefix`, `ExplicitlyNoTablePrefix`.

`PluginHelpers.ReadManifest` enforces non-empty `Name`, `Version` and `AssemblyFile` — **but not
`Key`**. `PluginHelpers.SortManifests` is a Kahn topological sort over `Dependencies`, throwing on a
missing dependency or a cycle. `GetAssemblyPath` falls back to any `*.dll` whose filename contains the
plugin key.

## The load context — what it does and does not isolate

`Vortex.Runtime/AssemblyProcessing/ByteLoadingAlc.cs`:

```csharp
class ByteLoadingAlc : AssemblyLoadContext(isCollectible: true)
```

A genuine collectible ALC. `AssemblyMemoryLoader.LoadFromBytes` reads **every `*.dll` in the plugin
folder into memory first**, then `LoadFromStream`s them — so no file locks, and the plugin DLL can be
rebuilt while loaded. Anything not in that dictionary goes through `AssemblyDependencyResolver` (from
`.deps.json`), also byte-loaded.

### What you do **not** get

- **No fault isolation.** The plugin runs on the same threads in the same process. An unhandled
  exception in a plugin handler is caught only where the pipeline catches it
  (`EnvelopeHost.OnHandlerInvokeError`), not by any sandbox.
- **No permission boundary.** `HostServices.GetRequiredService<T>()` forwards to the **full host
  `IServiceProvider`** with no allow-list. A plugin can resolve `VortexDbContext`, `IRevisionManager`,
  anything.
- **Type identity is shared only by omission.** `ByteLoadingAlc.Load` byte-loads any DLL sitting in
  the plugin folder — **including `Vortex.Primitives.dll` if it ships there**, which would give the
  plugin its *own* `IVortexPlugin` type and make `AssemblyExplorer.FindType` find nothing. This is a
  packaging rule, not an enforced one: `PluginActivationFailureTests.CopyTestPluginToTempFolder`
  copies **only** the plugin DLL for exactly this reason.
- **Unload is best-effort with a ceiling.** `UnloadAndWaitAsync(alc, 5000, ct)` loops `GC.Collect()` /
  `WaitForPendingFinalizers` for up to 5 s against a `WeakReference`; on timeout the caller logs
  *"Possible memory leak from retained type references"*.

## Activation is transactional

The 2026-07-15 audit's "plugin ALC leak" is **fixed**, and the fix is documented in-code:
`BuildEnvelopeOrUnloadAsync`'s doc records the old behaviour — only the ALC was unloaded while the
service provider, started hosted services and published exports stayed in place, so the ALC could
never actually collect.

`ActivationRollback` is a compensation stack: `Push(step, undo)` / `Commit()` / `UnwindAsync()`
(reverse order, each step independently guarded and logged).

`BuildEnvelopeAsync` pushes compensations in this order:

```
dispose plugin instance
  → dispose service provider
    → restore previous exports
      → (bind exports)
        → stop each hosted service, pushed only after that service starts successfully
          → stop plugin
```

Failure at any step ⇒ `UnwindAsync()` in reverse, **then** ALC unload, then rethrow.

> **Migrations are deliberately not compensated.** The comment: running `IPluginDbModule.UninstallAsync`
> because a hosted service failed would turn a recoverable startup error into data loss.

`StopAndTearDownAsync` guards each stage independently — its doc records the old single try/catch that
let one throw skip the rest.

Export binds are journaled and reversible (`ExportJournal`, `IExportSlot.SwapDeferredAsync` /
`RollbackAsync` / `DisposeSupersededAsync`). `SwapDeferredAsync` never disposes the displaced instance
eagerly, because that makes a rollback *"a second, subtler outage"*.

## The activation sequence

```
PluginManager.LoadAllAsync
  ├─ discover  →  topological sort by Dependencies
  └─ per plugin:
       byte-load ALC
       find and Activator.CreateInstance the single IVortexPlugin
       verify inst.Key == manifest.Key (ordinal)
       build the plugin's own ServiceProvider
         ValidateScopes = true, seeded with the manifest,
         IPluginCatalog, IHostServices, prefixed logging
       BindExportsAsync
       ProcessMigrationsAsync   (scoped IPluginDbModule.MigrateAsync)
       start each IHostedService, pushing a stop-compensation after each success
       plugin.StartAsync
       rollback.Commit()
       stage
  → assembly processing (bounded parallel)
  → publish only what fully activated
  → unload plugins no longer on disk
```

**Staged publication**: an envelope reaches `_live` only after assembly processing *also* succeeded.
`GetLiveKeys()`'s doc states the invariant — a key appears only after every activation step succeeded.

Concurrency: a global `_reloadGate` plus a per-key `SemaphoreSlim`. Dependency safety: reload and
unload are refused while dependents are live; `ReloadAsync` refuses if a dependency is not active;
`UnloadAllAsync` unloads leaves first, falling back to "all remaining" if a cycle exists.

A per-plugin failure is contained — `LoadAllAsync` logs and `continue`s.

## The four extension points

A scanned assembly is examined for exactly four things. There are exactly four
`IAssemblyFeatureProcessor` implementations, and this is the whole surface:

| Processor | Scans for |
|---|---|
| `MessageFeatureProcessor` | `IMessageHandler<>` / `IMessageBehavior<>` |
| `EventFeatureProcessor` | `IEventHandler<>` / `IEventBehavior<>` |
| `RoomObjectLogicFeatureProcessor` | `[RoomObjectLogic]` furniture logic |
| `WiredVariableFeatureProcessor` | wired variables |

**Nothing scans for `IRevision`** → [Protocol revision plugins](protocol-revision-plugins.md).

Every registration returns an `IDisposable`, and **the disposable is the deregistration** — which is
what makes unload clean.

> **Trap:** `AssemblyExplorer.FindClosedImplementations` skips anything not `public`. An `internal` or
> nested handler compiles, ships, and is silently never registered.

## Exports

Cross-plugin service publishing with live swap:
`IExportBinder` / `IExport<T>` / `IPluginCatalog.GetExport<T>(exportKey)` / `Subscribe(Action<T>)`.

## Logging

`ConfigurePrefixedLogging(services, host, manifest.Name)` replaces the open-generic `ILogger<>` with
`PrefixedLogger<>`, backed by `PrefixedLoggerFactory(hostLoggerFactory, prefix)` — so a plugin's log
lines are attributable to it while still flowing through the host's factory and level rules.

## Hot reload

`PluginHotReloadService` watches `PluginFolderPath` (recursive) and each dev path (non-recursive) for
`manifest.json`, `*.dll`, `*.pdb`, `*.deps.json`; debounces (`max(100, DebounceMs)`, default 500) then
calls `LoadAllAsync`, retrying up to 3 times on `IOException`/`InvalidDataException` (file churn) with
a 250 ms delay.

> **It is almost certainly off by default, even in Development.**
> `Vortex.Plugins/Extensions/ServiceCollectionExtensions.cs` gates registration on a **raw**
> `pluginSection.GetValue<bool>("HotReloadEnabled")`, which returns `false` when the key is absent —
> while `PluginConfig.HotReloadEnabled` defaults to `true`. Neither appsettings file contains a
> `Vortex:Plugin` section at all. Not runtime-verified, but the code path is unambiguous.

## The worked example

`Vortex.Plugins.TestPlugin` is deliberately tiny and references `Vortex.Primitives` only.

It picks its failure point from the **environment variable** `VORTEX_TEST_PLUGIN_FAILURE`, because —
as `FailurePoint.cs` explains — the plugin is byte-loaded into its own collectible ALC and therefore
**does not share statics** with the test host. It writes a breadcrumb trail to
`VORTEX_TEST_PLUGIN_TRACE`.

Failure points: `BindExports`, `HostedServiceStart`, `PluginStart`, `Migration`.

`Vortex.Plugins.Tests/PluginActivationFailureTests.cs` drives the **real** `PluginManager` against a
**real byte-loaded** plugin — no mocks in the activation path. 12 cases: healthy load; each of the 4
failure points never appearing as live; service-provider disposal on failure; "second hosted service
fails ⇒ the first is stopped"; "plugin start fails ⇒ every hosted service stopped"; plugin-instance
disposal; migration failure starts no hosted service; recovery after a failure; double-load leaves
exactly one activation; a failed reload leaves nothing live.

## The event bus (not the packet pipeline)

Both are instances of the same `EnvelopeHost` machinery. Keeping them distinct matters:

| | Event pipeline | Packet pipeline |
|---|---|---|
| Envelope | `IEvent` (empty marker) | `IMessageEvent` |
| Context | `EventContext` — `Cancel`, `CancelReason`, `CorrelationId`, `Items` | `MessageContext(session, playerId, roomId)` |
| Short-circuit | `ctx => ctx.Cancel` — **cancellable** | none |
| Entry | `EventSystem.PublishAsync` / `PublishCancellableAsync` | driven from the socket |
| Missing handler | silent | logs a warning |

Both use parallel handlers, `[Order]`-sorted behaviours, and inheritance dispatch. Failures route to
`IErrorGroupingSink` under `event-registry.*` vs `message-registry.*`.

134 `IEventPublisher` references, 17 event families, 162 `IEventHandler<>` implementations — **19 of
the 26 handler files live in `Vortex.Observability/Events/`**. Audit and forensics is the dominant
consumer of the event bus.

## Configuration

| Key | Default |
|---|---|
| `Vortex:Plugin:PluginFolderPath` | `<base>/plugins` |
| `Vortex:Plugin:DevPluginPaths` | `[]` |
| `Vortex:Plugin:HotReloadEnabled` | class default `true`, **effective `false`** (see above) |
| `Vortex:Plugin:DebounceMs` | 500 |

## Known unknowns

- **Unknown:** what enforces `PluginManifest.TablePrefix`. **Nothing in `Vortex.Plugins` reads it** —
  it is passed to the plugin and enforced, if at all, plugin-side.
- **Unknown:** behaviour with a duplicate or empty manifest `Key`. `ReadManifest` does not validate
  it, and it reaches `SortManifests`' `ToDictionary` and the `_live` dictionary.
- **Unknown:** how a plugin shipping grain types fails. `README.md` states the limitation ("grain type
  registration happens at silo startup") but nothing in code enforces it.
- **Unverified:** the whole plugin persistence path. No in-repo plugin declares a DbContext; it is
  exercised only by fakes. → [Database overview](../07-database/overview.md)

## Sources

- `Vortex.Plugins/{PluginManager,PluginBootstrapper,PluginHotReloadService,PluginHelpers,ActivationRollback,HostServices}.cs`
- `Vortex.Plugins/Exports/{ExportRegistry,ExportJournal,ExportBinder,ReloadableExport,IExportSlot}.cs`
- `Vortex.Plugins/Extensions/ServiceCollectionExtensions.cs`
- `Vortex.Primitives/Plugins/{IVortexPlugin,IHostPluginModule,IHostPlugin,IHostServices,IPluginCatalog,IPluginDbModule,PluginManifest}.cs`
- `Vortex.Runtime/AssemblyProcessing/{ByteLoadingAlc,AssemblyMemoryLoader,AssemblyProcessor,AssemblyExplorer,IAssemblyFeatureProcessor}.cs`
- `Vortex.Events/**`, `Vortex.Pipeline/EnvelopeHost.cs`
- `Vortex.Plugins.TestPlugin/{TestPlugin,FailurePoint}.cs`
- `Vortex.Plugins.Tests/{PluginActivationFailureTests,ActivationRollbackTests,ExportRollbackTests}.cs`
