---
name: new-dashboard-page
description: Ordered checklist for adding a dashboard capability or admin page — the four files that must move together, plus the ergonomics rules that get missed on every new page.
disable-model-invocation: true
---

# New dashboard page / capability

A new page touches four declaration files plus the server and client halves. The server half is
self-checking (`CapabilityDeclarationTests`). The client half is checked by the PostToolUse hook
(`scripts/hooks/check-dashboard-capabilities.mjs`) — but the hook only fires on files you actually
edit, so work the list, don't rely on it to find what you forgot to open.

Full walkthrough: `docs/walkthroughs/add-a-dashboard-page.md`. Reference pair:
`DashboardEndpoints.Quests.cs` + `DashboardApiService.Quests.cs`.

## 1. Declare the capability (four files)

1. `Vortex.Primitives/Permissions/Capabilities.cs` — the `const string` **and** its entry in
   `Capabilities.Dashboard.All`. Do not add a per-file copy anywhere else: that duplication is what
   used to throw `InvalidOperationException: The AuthorizationPolicy named '<capability>' was not
   found` at startup. `DashboardWebHost` and `DashboardAuthService` both read the one list.
2. `Vortex.Dashboard.Web/src/lib/dashboardPermissions.js` — `CAPABILITIES` **and**
   `ROUTE_PERMISSIONS`.
3. `Vortex.Dashboard.Web/src/lib/routes.js` — a `NAV` row. `load` must be a literal
   `() => import('../pages/X.svelte')`; Vite cannot code-split a computed path.
4. `Vortex.Dashboard.Web/src/lib/locales/en.js` **and** `fr.js` — the page block **and** the `nav.*`
   labels named by `labelKey`/`shortKey`. The two files must stay structurally identical: `en.js` is
   every other locale's fallback, so a key missing there breaks twice.

## 2. Server side

- Read path: `DashboardApiService.<Domain>.cs`. Write path: an `I<Domain>AdminService`.
- **No direct DB writes from `DashboardOperationsService`** — route through the admin service.
- Any admin write must reload the manager-grain cache it feeds.
- Register the endpoint in `DashboardEndpoints.cs`; an unregistered endpoint is a 404 nobody notices.
- If the endpoint injects a service, add its type to `DashboardWebHost.ForwardedServiceTypes`. The
  dashboard has its own DI container: an unforwarded type is read as a request body and kills the
  whole dashboard at startup. `DashboardEndpointServiceTests` catches this — let it.

## 3. Ergonomics — the part that gets missed every time

This is not polish. It is the difference between a usable page and an unusable one, and it has been
missed on every new page so far.

- **Every create and every edit goes in `<Drawer>`.** Never a form panel spliced into the page under
  the table, never an editor expanded inside a row. An inline form pushes the list off screen — the
  thing you were looking at when you decided to change a number — and a row editor breaks the grid
  around it. `let drawer = $state(null)`; a `New …` button in the `panel-head` sets
  `{ mode: 'create', form: empty() }`; the row action sets `{ mode: 'edit', id, form: {...row} }`;
  the buttons go in `{#snippet actions()}` so Save cannot scroll away. Modals are for short
  confirmations and pickers only — answers, not editing. **This is missed more often than anything
  else on this list, including by people who had just read `Drawer.svelte`'s own comment.**
- **Filters get their own row above the table.** Not the `panel-head`, not mixed into the page
  actions. `<TableFilter bind:query shown total />` when the table is already fully loaded; a plain
  `<div class="filters">` holding the inputs when the search is the server's.
- **Buttons carry a label and no icon.** An add is `class="success"` (green), a destructive action
  `class="danger"`, a secondary one `class="ghost-button"`, a refresh `class="warning"`. The colour
  is the affordance; a lucide glyph beside the word is noise this dashboard does not use.
- **Show the artwork, never a bare id.** The read API adds `furnitureIconUrl = BuildFurniIconUrl(name)`
  (see `DashboardApiService.Catalog.cs`); render it with `<AssetImage src={...} />`. Same for avatars
  and guild badges via `DashboardAssetUrls`.
- **Never make an operator type an id.** Use `<PickerModal kind="furniture" />` or `kind="user"`,
  backed by `/api/v1/directory/furniture`. A bare number input means looking the id up elsewhere first.
- **Reads go through `createResource(key, loader, opts)`** (`src/lib/resource.js`, TanStack Query —
  the key IS the cache identity, and resources are objects, not stores).
- **Writes go through `createWriteOps(onSuccess)`** (`src/lib/writeOps.js`). It is the only write path.
- **Adding a section to an existing page? Convert that page to tabs in the same edit.** This is the
  trigger that gets missed — not the new page itself.
- Every page must beat reading the raw table. If it does not, it is not worth the route.

## 4. Validate

```bash
grep -rn "<an existing capability string>" --include=*.cs --include=*.js .   # same file set?
node scripts/hooks/check-dashboard-capabilities.mjs                          # parity, routes, locales
cd Vortex.Dashboard.Web && npm run lint                                      # npm run build misses undefined identifiers in markup
dotnet build Vortex.Main/Vortex.Main.csproj -t:VortexCloudFastCheck
```

Note: `.svelte` files are CRLF — a `$`-anchored regex silently matches nothing. Svelte styles are
component-scoped; only `src/styles.css` is global. The SPA is 100% Svelte 5 runes.
