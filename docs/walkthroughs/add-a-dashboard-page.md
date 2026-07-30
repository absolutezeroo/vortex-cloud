# Walkthrough: Add a Dashboard Page

Adding an admin surface to the dashboard means touching a **capability string that is
duplicated across six files**. Nothing cross-checks those copies: a miss compiles, the
test suite stays green, and the failure only appears at runtime — as a 403 for every
operator, or as this, thrown the moment the endpoint is hit:

```
InvalidOperationException: The AuthorizationPolicy named 'dashboard.<name>.read' was not found.
```

That exact error has shipped four times. The checklist below exists so it stops.

> Follow the hard boundaries in `CONTEXT.md` and the contract in `AGENTS.md`. The
> placement rules here restate where those files say each kind of code belongs.

---

## The capability checklist

A dashboard capability lives in **six** places. Before claiming a page works, grep an
existing capability across the repo and confirm the new one appears in the same set:

```bash
grep -rn "dashboard.config.read" --include=*.cs --include=*.js .
grep -rn "configRead" Vortex.Dashboard.Web/src/lib/
```

| # | File | What it does | Symptom when missed |
|---|------|--------------|---------------------|
| 1 | `Vortex.Primitives/Permissions/Capabilities.cs` | `const string` in `Capabilities.Dashboard` **and** an entry in the `All` array | Capability not enumerable; validation rejects it |
| 2 | `Vortex.Dashboard.API/Hosting/DashboardWebHost.cs` → `DashboardCapabilities` | **Builds one auth policy per entry** | `AuthorizationPolicy named '…' was not found` at request time |
| 3 | `Vortex.Dashboard.API/Security/DashboardAuthService.cs` → `DashboardCapabilities` | The effective capabilities granted at login | Page 403s for every operator |
| 4 | `Vortex.Dashboard.Web/src/lib/dashboardPermissions.js` | `CAPABILITIES` entry **and** a `ROUTE_PERMISSIONS` row | Route guard cannot resolve the capability |
| 5 | `Vortex.Dashboard.Web/src/lib/routes.js` | Component import + route row | Page unreachable, absent from the nav |
| 6 | `locales/en.js` **and** `locales/fr.js` | The page's translation block **and** the `nav.*` label pair | Raw keys render; the two files must stay structurally identical because `en.js` is the fallback for every other locale |

Steps 1–3 are C#; a `dotnet build` will **not** catch a missing entry, because every list
is just an array of strings the compiler is happy with.

---

## Server layers

### Read surface — `Vortex.Dashboard.API/Api/DashboardApiService.<Domain>.cs`

A partial of `DashboardApiService`. Reads only: query through `QueryAsync`, return
anonymous objects. Join to whatever gives the operator a name instead of a bare id, and
compute anything the page would otherwise have to guess (shares, totals, derived flags)
here rather than in the browser.

### Write surface — two files

- `Vortex.Dashboard.API/Operations/<Domain>OperationContracts.cs` — one request record
  per operation, each carrying a mandatory `Reason`.
- `Vortex.Dashboard.API/Operations/DashboardOperationsService.<Domain>.cs` — a partial of
  `DashboardOperationsService`. Every operation goes through `ExecuteAsync`, which stamps
  a correlation id and emits a durable audit event **whatever the outcome**.

Operations must never write to the database directly. They call a domain admin service.

### Domain admin service — `I<Domain>AdminService`

Interface in `Vortex.Primitives/<Domain>/`, implementation in the module that owns the
data (e.g. `Vortex.Players/<Domain>/`), registered in that module's `ConfigureServices`.

If the data is cached by a kept-alive manager grain, **every write must reload that cache
before returning**. A committed write that leaves the live cache stale is the
"DB write not reflected in live state" bug class called out in `AGENTS.md` — and the
reload failure must be logged and rethrown, never swallowed.

See `Vortex.Players/Quests/QuestAdminService.cs` and
`Vortex.Players/MysteryBox/MysteryBoxAdminService.cs` for the shape.

### Endpoints — `Vortex.Dashboard.API/Hosting/DashboardEndpoints.<Domain>.cs`

A partial of `DashboardEndpoints` exposing `Map<Domain>Reads` and
`Map<Domain>Operations`, each endpoint declaring its capability. Both must then be called
from `DashboardEndpoints.cs` (`MapReads` / `MapOperations` regions) — a mapper nobody
calls is another silent no-op.

Validate the body in the endpoint (ids > 0, required strings non-empty, `HasReason`) and
return `invalid_request` rather than letting a malformed body reach the service.

---

## Front end

`Vortex.Dashboard.Web/src/pages/<Domain>Page.svelte`, plus the route/permission/locale
wiring above. Reuse the shared pieces instead of re-rolling them:

- `OpResult.svelte` — renders an operation result.
- `ConfirmReasonModal.svelte` — captures the mandatory reason. Prefer it over a reason
  field per form: the audit trail then cannot be skipped by a stray Enter key.
- `AccessDeniedNotice.svelte` — shown when a read comes back permission-denied.
- `PickerModal.svelte` (`kind="user"` / `kind="furniture"`) — searches the live directory and hands
  back `{ id, name, online }`. Never ask an operator to type a player name into a text box: the
  operation should carry an id, so a rename or a typo cannot misfire it.
- `hasDashboardCapability($identity, CAPABILITIES.ops<Domain>Manage)` gates every write
  control. The server enforces it too; this only hides what the operator cannot use.

The SPA is built into the API's static assets, so a page change is not live until:

```bash
cd Vortex.Dashboard.Web && npm run build   # emits into Vortex.Dashboard.API/Assets/
```

Commit the regenerated `Assets/` output together with the source change.

---

## Verifying

```bash
dotnet build Vortex.Main/Vortex.Main.csproj -t:VortexCloudFastCheck
dotnet build Vortex.Main/Vortex.Main.csproj -t:VortexCloudQualityGate
cd Vortex.Dashboard.Web && npm run build
```

Then **restart the emulator**. A running `Vortex.Main` holds its own DLLs, so the build
silently keeps serving the previous binary — a policy error that "won't go away" after a
fix usually just means the host was never restarted.

---

## Anti-patterns this walkthrough exists to prevent

- Adding a capability to `Capabilities.cs` only, and discovering the missing policy in
  production.
- Writing to the database from an operation instead of going through an admin service.
- Committing a write without reloading the manager-grain cache it feeds.
- Editing `en.js` without mirroring the structure in `fr.js`.
- Shipping a page without rebuilding the SPA bundle.
