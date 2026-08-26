# Dashboard capabilities and authorization

## Purpose

The capability model, the four files a capability string lives in, and exactly which guard covers
which half.

## The model

Authorization is **capability-based, not rank-based**. `SecurityLevelType` exists but is client-UI
only (`Vortex.Primitives/Permissions/SecurityLevelPolicy.cs` says so in its doc).

`Capabilities.Dashboard.All` is the single server-side list — **52 capabilities**. Both
`DashboardWebHost.ConfigureAuth` and `DashboardAuthService.HasDashboardAccess` read it; there is no
per-file copy. Reintroducing one is what used to throw
`InvalidOperationException: The AuthorizationPolicy named '<capability>' was not found` at runtime.

One policy per capability:

```csharp
// DashboardWebHost.ConfigureAuth
foreach (var capability in Capabilities.Dashboard.All)
    options.AddPolicy(capability, p => p
        .RequireAuthenticatedUser()
        .RequireClaim(CapabilityClaimType, capability, Capabilities.Wildcard));
```

`Capabilities.Wildcard` (`"*"`) is accepted as an **alternate claim value** — that is how `*`
satisfies every policy, without a special case in the policy builder.

`PermissionSet.IsSuperUser` / `Has` short-circuit on it.

### Only `owner` holds `*`

`Vortex.Authentication/Permissions/DefaultRoles.cs` seeds five roles:
`new RoleSeed("owner", "Owner", [Capabilities.Wildcard])`, plus `player`, `moderator`, `economy`,
`admin`. Those four cover **9 of the 52** dashboard capabilities between them — `OverviewRead`,
`AuditRead`, `PlayersRead`, `EconomyRead`, `FurnitureRead`, `OpsKickPlayer`, `OpsGrantCurrency`,
`OpsGrantItem`, `OpsManageVouchers` — so out of the box only `owner` reaches most pages.

## Login and session

`POST /api/login` (anonymous, rate-limited, partitioned on remote IP) → `DashboardAuthService.LoginAsync`.

Credentials and TOTP are verified in **one call** — `IAccountAuthenticator.VerifyCredentialsAsync(email, password, code, ct)`
— so the second factor is enforced identically for the dashboard and `Vortex.WebApi`. Wall 5 of
`check-architecture-walls.mjs` forbids a second `BCrypt.Verify` anywhere outside
`Vortex.Authentication`, precisely so the SSO-issuing login cannot fall behind on it.

Outcomes: `InvalidCredentials` / `MfaRequired` / `InvalidCode` (all 401, all sessionless) /
`Forbidden` (403) / `Authenticated`.

> **Authorization runs after the factor, deliberately.** `LoginAsync` resolves the `PermissionSet` and
> applies `HasDashboardAccess` only in the `Authenticated` branch — so a wrong password and a
> valid-but-unauthorized account are not distinguishable by timing or by response shape before the
> factor is checked.

Session: `DashboardSessionStore` over `AccountSessionStore<DashboardSessionState>`. A 256-bit opaque
token in an `HttpOnly`, `SameSite=Strict`, `Secure`-when-HTTPS cookie named `dash_session`. Lifetime
`DashboardSessionLifetimeMinutes` (480, floored to 5). **In-memory only — cleared on restart.**

### Capabilities are re-resolved per request

`DashboardAuthenticationHandler.HandleAuthenticateAsync` → `DashboardAuthService.ResolveAsync` →
`IPermissionService.ResolveForAccountAsync`. Nothing is baked into the cookie.

> The comment "revoking a role takes effect immediately" is exact **only when the writer invalidates**.
> `PermissionService.CacheTtl` is 60 seconds, so the real ceiling is ≤60 s otherwise.
> `StaffAdminService` does call `InvalidateAccount` for every affected account on every role change —
> so for the dashboard's own writes, immediately is accurate.

## Step-up MFA

`DashboardEndpoints.MapPost` consults `StepUpRequired.Capabilities` — a `FrozenSet` of **9**
capabilities — and attaches `StepUpRequired.Instance` metadata plus `DashboardStepUpFilter`.

> **The decision is by capability, not by route.** A future route inheriting one of those capabilities
> is gated automatically.

The filter reads nothing from the body: the session cookie, `DashboardSessionStore.SteppedUpAtUtc`,
and the clock. Window is `DashboardStepUpMinutes`; **0 disables step-up entirely**.

Three outcomes: pass, `mfa_step_up_required` (403), `mfa_enrolment_required` (403). The SPA retries
once on the first and never on the second.

Step-up capabilities: currency grant, item grant, voucher manage, forensics purge, staff manage,
config manage, database backup, server console, server control.

Notably **not** step-up gated: kick, ban, mute, trading lock, CFH manage — reversible and
time-pressured, so the friction is deliberately absent.

## The four-file duplication

| # | File | What must be added |
|---|---|---|
| 1 | `Vortex.Primitives/Permissions/Capabilities.cs` | the `const string` **and** `Capabilities.Dashboard.All` |
| 2 | `Vortex.Dashboard.Web/src/lib/dashboardPermissions.js` | `CAPABILITIES` **and** `ROUTE_PERMISSIONS` |
| 3 | `Vortex.Dashboard.Web/src/lib/routes.js` | a route row with `load: () => import('<literal>')` — pages are code-split, and the path must be a literal or Vite cannot split it |
| 4 | `Vortex.Dashboard.Web/src/lib/locales/en.js` **and** `fr.js` | page block **and** `nav.*` labels; the two files must stay structurally identical (`en.js` is every other locale's fallback) |

> `CONTEXT.md` says **six** files. `AGENTS.md` says four and lists four. `AGENTS.md` is the one that
> matches the hook.

## Which guard covers which half

This is the part worth memorising.

| Guard | Covers |
|---|---|
| `CapabilityDeclarationTests` (4 facts) | the **server list** — every declared `dashboard.*` constant is in `All`, all are namespaced and unique, no duplicates |
| `check-dashboard-capabilities.mjs` | the **client half** — files 2, 3 and 4 |
| `DashboardAuthorizationMatrixTests` | the **route ↔ capability pairing** |
| *(nothing)* | a client route pointing at a server route that does not exist |

### What the hook actually checks

Three sections, read from the script:

1. **Capabilities, both directions.** Regexes `"(dashboard\.[a-z0-9_.]+)"` out of `Capabilities.cs`
   as *text* and imports `dashboardPermissions.js`. Fails on a server capability missing from
   `CAPABILITIES` ("the page it guards is hidden from every operator") **and** on a client capability
   with no server constant ("the policy does not exist server-side").
2. **Routes.** Parses `routes.js` as **text** (importing it would pull in `.svelte`), slicing from
   `export const NAV`. Per row it fails on: a `ROUTE_PERMISSIONS.<key>` that does not exist; no
   `ROUTE_PERMISSIONS` reference at all; neither `component:` nor a literal `load: () => import(...)`;
   and **a lazy import path resolving to a file that does not exist**. It *warns* on a declared
   `ROUTE_PERMISSIONS` key no NAV row uses — currently one, `moderationActions`.
3. **Locales.** Flattens `en.js` / `fr.js` and fails on any key present in one and not the other, both
   directions, plus every `labelKey` / `shortKey` a NAV row names.

Because section 1 regexes only the `const` declaration, **it cannot tell whether the constant reached
`Capabilities.Dashboard.All`** — that is `CapabilityDeclarationTests`' job alone.

Live at this commit:

```
check-dashboard-capabilities: OK (52 capabilities, 43 routes, 2571 locale keys)
```

Where it runs: `post-edit.mjs` (PostToolUse, only when the edited path matches one of the four files)
and `VortexCloudFastCheck` (so any author or tool is covered).

## The authorization matrix

`Vortex.Dashboard.Tests/Hosting/authorization-matrix.txt` is a **generated, test-enforced snapshot** of
every route → capability pair — 215 routes: 80 GET, 135 POST (128 of them
`/api/v1/operations/*`, 5 `/api/v1/account/*`, 2 anonymous).

`DashboardAuthorizationMatrixTests` asserts four things:

- the matrix equals the snapshot (writing `authorization-matrix.actual.txt` on failure)
- every demanded capability is in `Capabilities.Dashboard.All`
- only `/api/login`, `/api/logout`, `/api/me` are unversioned
- only login and logout are anonymous, and **no route is unguarded**

**51 of the 52 capabilities gate at least one route.** The exception is
`dashboard.ops.server.control`, which by design gates a *console command* rather than a route
(`Vortex.Main/Console/ConsoleCommandDispatcher.cs`) — stated in `DashboardStepUp.cs`.

> ### One operation has no route
>
> `DashboardOperationsService.ResetAccountMfaAsync` (action `ops.staff.mfa.reset`, request type
> `ResetAccountMfaRequest`) has **no endpoint**. `grep -rn "ResetAccountMfa"` across `.cs`, `.js` and
> `.svelte` returns only the declaration, its contract, and a prose reference in
> `DashboardEndpoints.Account.cs` describing it as the place where clearing somebody else's factor
> "lives where it belongs". It does not: `DashboardEndpoints.Staff.cs` maps 9 routes and none is
> `/staff/mfa/reset`.
>
> **The operator-recovery path for a lost authenticator is currently unreachable from the dashboard.**

## Capability groups

**Read-only (27):** `overview.read`, `audit.read`, `economy.read`, `players.read`, `furniture.read`,
`catalog.read`, `catalog.purchases.read`, `chatlogs.read`, `groups.read`, `pets.read`, `cfh.read`,
`wired.read`, `social.read`, `staff.read`, `collectibles.read`, `achievements.read`, `bots.read`,
`navigator.read`, `quests.read`, `polls.read`, `prize_pools.read`, `mystery_box.read`,
`targeted_offers.read`, `config.read`, `performance.read`, `benchmark.read`,
`server.console.read`.

**Write** — the full table with routes, class and step-up status is on
[Operations](operations.md).

## Other invariants

- **Asset serving is path-confined.** `DashboardAssetStore.IsSafeAsset` rejects `/`, `\` and `..`, then
  requires the name to resolve to an embedded resource.
- **Ambient security context.** `ActorSecurityContext` (an `AsyncLocal`) carries account, session id,
  resolved permissions, `SteppedUpAtUtc` and correlation id. `ActorSecurityContextTests` proves scope
  publish/restore, nesting and concurrency isolation.

## Sources

- `Vortex.Primitives/Permissions/{Capabilities,PermissionSet,SecurityLevelPolicy}.cs`
- `Vortex.Authentication/Permissions/{DefaultRoles,PermissionService,StaffAdminService}.cs`
- `Vortex.Dashboard.API/Security/{DashboardAuthService,DashboardSessionStore,DashboardAuthenticationHandler,ActorSecurityContext}.cs`
- `Vortex.Dashboard.API/Hosting/{DashboardWebHost,DashboardEndpoints,DashboardStepUp}.cs`
- `Vortex.Dashboard.Tests/Hosting/{DashboardAuthorizationMatrixTests,DashboardStepUpTests,ActorSecurityContextTests}.cs`
- `Vortex.Dashboard.Tests/Hosting/authorization-matrix.txt`
- `Vortex.Authentication.Tests/Permissions/CapabilityDeclarationTests.cs`
- `scripts/hooks/check-dashboard-capabilities.mjs`, `post-edit.mjs`
- `AGENTS.md` — "Add dashboard capability or admin page"
- `docs/walkthroughs/add-a-dashboard-page.md`
