# Database overview

## Purpose

One context, how everything gets hold of it, and the version pin that surprises people.

## One production DbContext

| Type | Role |
|---|---|
| `DbContextBase<TContent>` | applies the three model conventions in `OnModelCreating` |
| `VortexDbContext` | **the** application context: 145 `DbSet`s + relationship overrides |
| `PluginDbContextBase<TContent>` | base for plugin-owned contexts; forces every FK to `Restrict`, applies a table prefix |
| `VortexDbContextFactory` | `IDesignTimeDbContextFactory<VortexDbContext>` |

> The other four "DbContexts" a symbol scan reports (`FaultyDbContext`, `FaultyDbContextFactory`,
> `TestDbContextFactory`, `ThrowingDbContext`) are **fault-injection test harnesses**, not production
> contexts.

Model coverage is exact: **145 `DbSet<T>` declarations, 145 mapped entity classes** (plus the
`VortexEntity` base, which has no `DbSet`). No entity is orphaned from the context.

## Registration: pooled factory, never scoped

```csharp
// Vortex.Database/Extensions/ServiceCollectionExtensions.cs — AddVortexDatabaseContext
services.AddPooledDbContextFactory<VortexDbContext>(options => options
    .UseMySql(connectionString, ResolveServerVersion(...), o => {
        o.MigrationsAssembly("Vortex.Database");
        o.EnableRetryOnFailure(maxRetryCount: 3);
    })
    .AddInterceptors(new EntityChangeInterceptor()));
```

The comment records the measurement behind pooling: 20 000 create/dispose cycles took **1085 ms
unpooled vs 56 ms pooled**, with a `ponytail:` note that pool size stays at the default 1024.

**There is no `AddDbContext<VortexDbContext>` and no `AddScoped<VortexDbContext>` anywhere.** Every
consumer takes `IDbContextFactory<VortexDbContext>` and creates/disposes per operation:

```csharp
await using VortexDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct);
```

Never as a field. That uniformity is what makes the grain model work — a short-lived context per grain
turn.

`AddVortexDatabaseContext` is also the composition root for three things that are not schema:
`ICommerceJournal`, the `CommerceRelayService` hosted service, and
`IDatabaseBackupService` + `DatabaseBackupScheduler`.

## Who touches the database

| Project | Files injecting the factory |
|---|---|
| `Vortex.Players` | 19 |
| `Vortex.Progression` | 17 |
| `Vortex.Rooms` | 11 |
| `Vortex.Catalog` | 11 |
| `Vortex.Authentication` | 10 |
| `Vortex.Observability` | 8 |
| `Vortex.Collectibles` | 7 |
| `Vortex.Social` | 6 |
| `Vortex.WebApi` | 4 |
| `Vortex.Marketplace` / `Vortex.Navigator` / `Vortex.Inventory` / `Vortex.Furniture` / `Vortex.Dashboard.API` | 3 / 2 / 2 / 2 / 2 |

Four shapes of consumer: **grains**, **singleton reference-data providers** (14 of them),
**admin services** (write then reload), and **hosted services** (writers, sweepers, seeders).

### Packet handlers do not — proven three ways

Search scope: **all 558 `.cs` files under `Vortex.PacketHandlers/`**, excluding `bin`/`obj`.

1. **The missing reference.** `Vortex.PacketHandlers.csproj` carries an explicit comment where it
   would be: *"Deliberately NO reference to Vortex.Database… Keeping the reference out makes the
   compiler enforce that boundary instead of leaving it to review."*
2. **Grep.** Zero `DbContext`, `DbSet`, `EntityFrameworkCore`, `SaveChanges`, `ExecuteUpdate`,
   `ExecuteDelete` or `FromSql` tokens. The only `Entity` hits are three prose comments.
3. **Tests.** `Vortex.Hosting.Tests/ProjectBoundaryTests.cs` —
   `PacketHandlers_DoNotReferenceTheDatabaseProject` and `PacketHandlers_ContainNoDatabaseUsings`.

> **Caveat worth recording:** `Vortex.PacketHandlers` *does* reference `Vortex.Catalog`,
> `Vortex.Players`, `Vortex.Progression` and `Vortex.Social`, all of which reference
> `Vortex.Database`. C# project references are transitive by default and none is marked
> `PrivateAssets`, so the *types* remain reachable. The csproj comment overstates the compiler's role
> — **the `using`-scan test is the check that actually holds.**

## The EF Core 9 / .NET 10 pin

```xml
<!-- Directory.Packages.props -->
<!-- Pinned to the EF Core 9 line: Pomelo.EntityFrameworkCore.MySql has no EF Core 10-compatible
     release yet (tracked upstream: PomeloFoundation/Pomelo.EntityFrameworkCore.MySql#2007).
     EF Core 9 packages run fine on the net10.0 TFM/runtime; bump these once Pomelo ships. -->
```

`Microsoft.EntityFrameworkCore.*` **9.0.8** · `Pomelo.EntityFrameworkCore.MySql` **9.0.0** ·
`MySqlConnector` **2.4.0** (pinned explicitly because `Vortex.Main` references `MySqlConnectorFactory`
directly) · TFM `net10.0`.

**Bump both EF Core and Pomelo together, never one without the other.**

A second consequence: `Microsoft.CodeAnalysis.*` is pinned to **5.6.0** to reconcile
`Microsoft.Orleans.Sdk 10.2.1` (needs Roslyn ≥ 5.0.0) against
`Microsoft.EntityFrameworkCore.Design 9.0.8` (pulls Roslyn 4.8.0).

## Model configuration: annotations plus conventions

> **There is no `IEntityTypeConfiguration` in this codebase**, and no
> `ApplyConfigurationsFromAssembly`. Grep returns nothing for either.

Three layers:

### 1 · Data annotations on the entity

`[Table]`, `[Column]`, `[Index(…, IsUnique)]`, `[ForeignKey]`, `[InverseProperty]`, `[MaxLength]`,
`[DatabaseGenerated]`, plus two repo-local attributes: `DefaultValueSqlAttribute` and
`EnumStorageAttribute`.

### 2 · A reflection sweep and three global conventions

`Vortex.Database/Extensions/ModelBuilderExtensions.cs`:

| Method | Effect |
|---|---|
| `ApplyDefaultAttributesFromEntities` | walks entity types **already in the model** (no assembly hardcoding) and turns `[DefaultValueSql]` / `[DefaultValue]` / `[EnumStorage]` into `HasDefaultValueSql` / `HasDefaultValue` / `HasConversion<int\|long\|string>` |
| `ApplyConventions` | every `string` → `MaxLength ?? 512`; every `DateTime` → a UTC-normalising `ValueConverter`; every `decimal` → precision 18 scale 6 |
| `ApplySoftDeleteQueryFilter` | a global `DeletedAt == null` filter on every non-owned `VortexEntity` |
| `ApplyTablePrefix` | plugin contexts only |

### 3 · One central `OnModelCreating`

`VortexDbContext.OnModelCreating` (~260 lines) is **for relationships and delete behaviour only** —
the decisions annotations cannot express, each with its reasoning inline. The one property-level
exception is `CatalogPageEntity.Layout`'s enum ↔ `varchar(50)` conversion.

→ [Ownership boundaries](ownership-boundaries.md) for what those decisions encode.

`VortexEntity` is the base for every mapped entity: `id`, `created_at` (`Identity`), `updated_at`
(`Computed`), `deleted_at`.

## Plugin persistence

`AddPluginDatabaseContext<TContext, TModule>` is a **scoped** `AddDbContext<TContext>` with
`MigrationsHistoryTable($"__EFMigrationsHistory_{prefix}")` and the same `EnableRetryOnFailure(3)`.

Prefix derivation: `PluginManifest.TablePrefix`, else the initials of the dash-split `Key`, else empty
when `ExplicitlyNoTablePrefix`.

> **No in-repo plugin declares a DbContext.** `Vortex.Plugins.TestPlugin`'s `TestDbModule` is a
> trace-only stub, so the whole plugin persistence path — prefixing, per-plugin migration history,
> `UninstallAsync`'s prefixed `DROP TABLE` — is exercised only by fakes. Whether it works against a
> real plugin schema is **untested**.

## Configuration

| Key | Default / note |
|---|---|
| `Vortex:Database:ConnectionString` | ships as a placeholder; `DatabaseConfigValidator` rejects empty and any of `CHANGE_ME` / `REPLACE_ME` / `YOUR_CONNECTION` / `PLACEHOLDER` at startup — and **never echoes the value**, because it carries the password |
| `Vortex:Database:MySqlServerVersion` | unset → `ServerVersion.AutoDetect` |
| `Vortex:Database:ServerVersion` | **design-time only**, read by `VortexDbContextFactory` |
| `Vortex:Database:Backup:*` | `Enabled=false`, `IntervalHours=24`, `RetentionCount=14`, `TimeoutMinutes=30` |
| `Vortex:Commerce:Recovery:*` | `SweepIntervalSeconds=30`, `RelayBatchSize=100`, `StuckAfterMinutes=10` |

> **Two keys, one concept, different names.** Runtime reads `MySqlServerVersion`; design-time EF reads
> `ServerVersion`. Setting one does nothing for the other. `README.md` documents the split; nothing
> reconciles it.

`ServerVersion.AutoDetect` **opens a real MySQL connection during DI configuration**, outside
`EnableRetryOnFailure`. That is why `docker-compose.yml` needs `depends_on: condition: service_healthy`.

Env prefix is `VORTEX__` (e.g. `VORTEX__Vortex__Database__ConnectionString`); **design-time EF tooling
reads unprefixed `Vortex__Database__*`**.

> Minor: `appsettings.Development.json` sets `Vortex:Database:LoggingEnabled`, which is not a property
> of `DatabaseConfig` — a dead key.

## Sub-pages

| Page | Covers |
|---|---|
| [Ownership boundaries](ownership-boundaries.md) | the rule for writing SQL; constraints that encode business logic |
| [Entities and relationships](entities-and-relationships.md) | the schema by bounded context |
| [Migrations](migrations.md) | naming, the offline recipe, seeding, and one orphan |

Exhaustive list: [Entity index](../generated/entity-index.md).

## Sources

- `Vortex.Database/Context/{VortexDbContext,DbContextBase,PluginDbContextBase,DesignTimeVortexDbContext}.cs`
- `Vortex.Database/Extensions/{ServiceCollectionExtensions,ModelBuilderExtensions}.cs`
- `Vortex.Database/Attributes/{DefaultValueSqlAttribute,EnumStorageAttribute}.cs`
- `Vortex.Database/Configuration/{DatabaseConfig,DatabaseConfigValidator,CommerceRecoveryConfig}.cs`, `Vortex.Database/Backup/DatabaseBackupConfig.cs`
- `Vortex.PacketHandlers/Vortex.PacketHandlers.csproj`, `Vortex.Hosting.Tests/ProjectBoundaryTests.cs`
- `Directory.Packages.props`, `AGENTS.md` "Foundational context"
