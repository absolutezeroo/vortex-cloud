# VORTEX CLOUD — Dashboard / API / Security — FINAL FROZEN

**Status: VALIDATED / FROZEN — canonical document for the Dashboard / API / Security workstream.**  
**Baseline:** `afc485be58ffd983b8d96430efe8aed620ad0ade` (`main`, 25 August 2026).  
**Relation to the runtime V4:** this document is independent of `Vortex_Runtime_Architecture_Workflow_V4_FINAL.md`. The runtime V4 explicitly excludes Dashboard/API/Security from its scope. No Runtime/Commerce/Rooms/Wired workstream may modify the decisions below as a side effect.

> This file consolidates the Dashboard decisions already validated. It does not constitute a new audit reopening. Any structural change to this document requires an explicit ADR and a targeted audit.

---

## 1. Objective

The Dashboard is a **privileged administrative control plane** for Vortex. It must not become a second business layer competing with the emulator, nor bypass the existing ownership boundaries.

The Dashboard must provide:

- robust operator authentication;
- revocable operator sessions;
- RBAC by **capabilities**;
- step-up MFA for sensitive operations;
- audited operator MFA recovery;
- centralized, auditable administrative operations;
- a durable ledger/audit of privileged mutations;
- a stable API with structured errors;
- complete moderation/CFH;
- a clear separation between Dashboard Web, API and Vortex domains;
- authorization matrix tests and quality gates.

---

## 2. Scope

### IN SCOPE

- `Vortex.Dashboard.API`
- `Vortex.Dashboard.Web`
- `Vortex.Dashboard.Tests`
- permission and administration contracts used by the Dashboard;
- operator authentication;
- MFA and MFA recovery;
- Dashboard sessions;
- staff/RBAC operations;
- moderation and CFH;
- administrative audit/ledger;
- HTTP contracts, IDs and errors;
- observability and control-plane tests.

### OUT OF SCOPE

- Rooms/Furniture/Wired refactor;
- V4 Commerce Consistency;
- changes to the Orleans ownership boundaries;
- generalized microservices;
- global event sourcing;
- generalized CQRS/MediatR;
- replacing the gameplay permission engine;
- mixing Dashboard PRs with V4 Runtime PRs.

---

## 3. Non-negotiable principles

1. **The Dashboard is a control plane, not a second business engine.** Writes call owner contracts/services/grains; they do not reimplement domain logic in the endpoints.
2. **Authorization by capabilities, never by role name or hardcoded rank.**
3. Operations that grant capabilities are themselves protected by a dedicated capability — notably `OpsStaffManage`.
4. **Every privileged mutation requires an identifiable actor, an operator reason and an audit trail.**
5. HTTP endpoints stay thin: parsing/authz/validation → operation service → domain contract.
6. MFA secrets are never returned or logged in clear outside the strictly necessary enrolment flow.
7. An MFA recovery does not bypass the audit: it is an explicit staff operation.
8. Operator sessions are **server-side and revocable**; a session's identity is not equivalent to the mere presence of a cookie on the client side.
9. API errors use a structured, stable format of the **Problem Details** / stable error code kind; no internal exception detail is exposed to the client.
10. The IDs carried by the API have explicit semantics; do not use ambiguous strings or UI indices as business identities.
11. CFH moderation stays connected to the real models/tickets; no second, Dashboard-only queue.
12. Critical operations are never "fire-and-forget" from the audit's point of view.
13. **Dashboard/API/Security stays FROZEN** until an ADR reopens it.

---

## 4. Frozen target architecture

```text
Vortex.Dashboard.Web
        |
        | HTTPS / API contracts
        v
Vortex.Dashboard.API
  ├── Authentication / Sessions / MFA
  ├── Authorization / Capabilities
  ├── HTTP Endpoints
  ├── DashboardOperationsService
  ├── Audit / Ledger
  ├── Read APIs / Projections
  └── CFH / Moderation API
        |
        | domain contracts / Orleans calls / application services
        v
Vortex domain owners
  ├── Players / Authentication / Permissions
  ├── Rooms / Moderation / CFH
  ├── Catalog / Economy / Reference Data
  └── other bounded owners
```

### Boundary

`DashboardEndpoint -> DashboardOperationsService -> owner contract`

and not:

`DashboardEndpoint -> DbContext -> arbitrary business mutation`.

An administrative read may use dedicated projections/read services; a mutation must respect the real owner.

---

## 5. Operator authentication & sessions

The repository already contains the dedicated subsystem:

- `Vortex.Dashboard.API/Security/DashboardAuthService.cs`
- `DashboardAuthenticationHandler.cs`
- `DashboardPrincipal.cs`
- `DashboardSessionStore.cs`
- `LoginRequest.cs`

Frozen decision:

- Dashboard authentication distinct from a mere player session;
- explicit Dashboard principal;
- server-side session;
- server-side invalidation/revocation possible;
- no sensitive authorization based solely on what the Web declares;
- the API always rebuilds the effective principal server-side.

### Target security context

The target model keeps the validated concept of an **ActorSecurityContext** at the administrative boundary: operator identity, session, effective capabilities, MFA/step-up state and the correlation metadata the audit needs.

The exact type name may change without an ADR as long as the semantics stay identical; what is frozen is the boundary: privileged operations must not depend on a mere `string actor` as their only long-term security context.

---

## 6. RBAC / capabilities

The Dashboard administers roles and capabilities through the existing permission contracts.

The baseline code notably has operations for:

- role creation / modification / deletion;
- replacing a role's whole capability set;
- role assignment / unassignment;
- sanction preset administration;
- operator MFA recovery.

These writes go through `DashboardOperationsService.Staff.cs`.

### Invariant

A permission change must record **the complete resulting state** when that is useful to the audit, not only a delta that cannot be interpreted in isolation.

Example already documented in the code: during `SetRoleCapabilitiesAsync`, the complete capability set is recorded in the operation detail so that the audit entry can answer on its own "what could this role do after the change?".

---

## 7. MFA, step-up and recovery

### Normal MFA

The Web has a dedicated MFA flow (`MfaModal.svelte`) and the API exposes the associated account/MFA operations.

### Step-up MFA

Frozen decision: a valid initial authentication is not necessarily enough for a highly sensitive operation. Operations selected as critical must be able to require a **recent MFA step-up**.

The step-up belongs to the operator security context, not to the business payload sent by the browser.

### MFA recovery

`DashboardOperationsService.ResetAccountMfaAsync` is the dedicated administrative route.

Validated invariant:

- dedicated `OpsStaffManage` capability;
- mandatory operator reason;
- audit of the `ops.staff.mfa.reset` action;
- explicit target account;
- no "backup MFA code" invented by the API;
- recovery clears the second factor through the owning MFA service.

That route is a **recovery path** and must stay more heavily audited than an ordinary change.

---

## 8. Administrative ledger / audit

The Dashboard must not have untraceable privileged mutations.

Every sensitive operation must produce an entry holding at least:

```text
OperationId / CorrelationId
Timestamp
Actor / Operator
Session identity
Effective capabilities / relevant security context
Action key
Reason
Target account/player/room/entity
Structured detail
Outcome
Failure code (if applicable)
```

### Consistency rule

When the business mutation and the audit live in the same local transactional boundary, **mutation + ledger entry must be committed together**.

When the operation crosses a remote owner/grain, the Dashboard logs the intent and the result with a stable correlation identifier; it does not pretend to create a cross-grain ACID transaction.

### Prohibitions

- audit as a text log only;
- optional reason on a sensitive write;
- destructive change with no actor;
- a journal containing secrets, passwords or MFA seeds;
- success returned before the required audit state is durable.

---

## 9. Operations boundary

`DashboardOperationsService` is the canonical boundary for Dashboard writes.

Endpoints must not grow into business services.

Target pattern:

```text
Endpoint
  -> authenticate
  -> authorize capability
  -> validate request/reason
  -> build ActorSecurityContext
  -> DashboardOperationsService
  -> owner service/grain
  -> ledger result
  -> stable HTTP result
```

Writes that themselves grant permissions stay separate from generic content operations.

---

## 10. API contract & Problem Details

Frozen decision: API errors are **structured and stable**.

Target:

```json
{
  "type": "urn:vortex:dashboard:<error-code>",
  "title": "Human readable category",
  "status": 403,
  "code": "mfa_step_up_required",
  "traceId": "...",
  "detail": "..."
}
```

The exact shape may follow `ProblemDetails`, but the invariants are:

- correct HTTP status;
- stable `code` for the Web;
- trace/correlation id;
- no stack trace;
- no raw SQL/EF/Orleans message;
- validation differentiated from authorization and from domain rejection;
- known domain errors are not logged as system faults.

---

## 11. IDs and contracts

Dashboard DTOs must distinguish:

- `AccountId`
- `PlayerId`
- `RoomId`
- `RoleId`
- `PresetId`
- CFH ticket IDs
- other domain IDs

Avoid:

- "id" with no context in sensitive APIs;
- implicit conversion of a UI ID into a different business ID;
- using a username as the authority key when the stable ID exists.

Strongly typed IDs may stay at the internal boundary; the JSON may stay numeric/string for existing compatibility, but the semantics must be unambiguous.

---

## 12. Moderation / CFH

CFH = **Call For Help**.

The baseline contains:

- `Vortex.Dashboard.API/Api/DashboardApiService.Cfh.cs`
- `Vortex.Dashboard.Web/src/pages/CfhQueuePage.svelte`
- `Vortex.Dashboard.Web/src/pages/CfhStatsPage.svelte`
- `Vortex.Rooms/CfhTicketService.cs`
- DB entities `CfhTicketEntity`, `CfhTopicEntity`, `CfhCategoryEntity`
- the moderation queue on the runtime side.

### Decision

The Dashboard **projects and operates on the existing CFH truth**. It does not create a second administrative source of truth.

Moderation writes use the corresponding capabilities and the operator ledger.

Stats are read models; they never become the owner of the CFH workflow.

---

## 13. Administrative host

Validated architecture decision: the Dashboard control plane must be able to be **hosted outside the main gameplay process**.

Goals:

- reduce the blast radius of a Web/admin error;
- allow deploying/restarting the Dashboard without restarting the game runtime;
- prevent an administrative HTTP server from directly increasing the main silo's attack surface;
- keep calls to the real owners through explicit boundaries.

This does not mean generalized microservices: it is a separation of the **control plane**, not a fragmentation of the domain.

`DashboardWebHost.cs` remains the Dashboard's HTTP composition point.

---

## 14. Web client

The Web:

- never decides effective permissions;
- hides/disables UI by capability for UX, but the API remains the authority;
- handles MFA/step-up as a user interaction, without storing the secret;
- consumes the API's stable error codes;
- does not reproduce the business rules of the grains/services.

Navigation permissions (`dashboardPermissions.js`, routes, etc.) must stay aligned with the API matrix.

---

## 15. Authorization matrix

The repository has a dedicated test matrix (`Vortex.Dashboard.Tests/Hosting/authorization-matrix.txt`).

Final criterion:

- every privileged endpoint appears in the matrix;
- the expected minimum capability is documented;
- capability absent -> refusal;
- correct capability -> access;
- staff/manage endpoints do not share an over-broad capability with content writes;
- step-up operations test the recent / absent / expired MFA cases.

A new Dashboard route with no matrix entry is a regression.

---

## 16. Mandatory tests

### Auth / session

- login success/failure;
- unknown/revoked/expired session;
- principal rebuilt server-side;
- logout/revocation;
- no trust in the permissions declared by the client.

### RBAC

- endpoint × capability matrix;
- role assign/unassign;
- full capability replacement;
- separation of `OpsStaffManage` from the other permissions.

### MFA

- enrolment/verification;
- step-up required;
- step-up valid;
- audited `ResetAccountMfaAsync` recovery;
- secret absent from logs/audit.

### Ledger

- actor/reason/action/target/detail present;
- failure outcome recorded per contract;
- no critical mutation without a durable entry;
- secret redaction.

### API

- stable error codes;
- Problem Details / standard shape;
- no stack trace;
- correlation id.

### CFH

- queue/stats read the same business truth;
- moderator actions respect capabilities;
- mutation audit.

---

## 17. Observability

Minimum metrics:

```text
dashboard.auth.success / failure
dashboard.session.active / revoked
dashboard.mfa.challenge / failure / recovery
dashboard.authorization.denied{capability}
dashboard.operation.total{action,outcome}
dashboard.operation.duration{action}
dashboard.audit.write.failure
dashboard.cfh.queue.size
dashboard.http.error{code}
```

Structured logs with a correlation id, never a secret.

Any alert on `dashboard.audit.write.failure` is a priority: an unaudited privileged mutation is a control-plane defect.

---

## 18. Explicitly forbidden anti-patterns

- authorization by `if (rank >= X)`;
- authorization only in Svelte;
- an endpoint that directly instantiates a `DbContext` to modify a domain it does not own;
- a "god endpoint" route that mixes several capabilities;
- unaudited MFA recovery;
- optional operator reason for sanctions/permissions/destructive writes;
- best-effort audit after returning success;
- stack traces in an HTTP response;
- MFA secrets/passwords in logs or ledger;
- Dashboard and Runtime V4 modified in the same large PR with no proven technical need;
- reopening the Dashboard architecture during Commerce/Wired PRs.

---

## 19. AI workflow for this workstream

This domain is **FROZEN**.

Before any change:

```text
1. Read this document.
2. Read AGENTS.md / CONTEXT.md / CLAUDE.md.
3. Compare the audited SHA to HEAD on:
   - Vortex.Dashboard.API/**
   - Vortex.Dashboard.Web/**
   - Vortex.Dashboard.Tests/**
   - Vortex.Primitives/Permissions/**
   - Vortex.Primitives/Authentication/**
4. If the watched_paths have not changed:
   -> do not re-audit the architecture.
5. If a change contradicts a FROZEN decision:
   -> STOP
   -> announce the conflict
   -> propose an ADR
   -> no implementation before acceptance.
```

Dashboard PRs must not be mixed with the V4 Runtime `C*`, `W*`, `G*`, `S*` PRs unless there is a documented concrete dependency.

---

## 20. Final acceptance criteria

The Dashboard/API/Security workstream is compliant when:

1. every sensitive route is protected by a capability on the API side;
2. the Web permissions are only a UX mirror;
3. permission operations use a dedicated staff capability;
4. every sensitive mutation requires an actor + reason;
5. the ledger provides a durable, correlatable trail;
6. MFA recovery is protected and audited;
7. the operations designated sensitive support MFA step-up;
8. operator sessions are server-side and revocable;
9. errors are structured, stable and free of internal leakage;
10. business IDs are unambiguous;
11. CFH uses the existing source of truth;
12. endpoints do not embed domain logic;
13. the administrative host can be separated from the gameplay process without changing the domains;
14. the authorization matrix covers every privileged route;
15. the Dashboard quality gates pass;
16. no decision in this document is implicitly modified by the V4 Runtime.

---

# FINAL STATUS

```text
Dashboard / API / Security
STATUS: ACCEPTED / FROZEN

Runtime / Gameplay / Commerce
STATUS: V4 CANONICAL

RULE:
The two documents are complementary.
Neither silently overrides the other.
A conflict requires an explicit ADR.
```

---

# Appendix A — Baseline sources

Repository: `https://github.com/absolutezeroo/vortex-cloud`  
Commit: `afc485be58ffd983b8d96430efe8aed620ad0ade`

## Dashboard API / Security

- `https://github.com/absolutezeroo/vortex-cloud/blob/afc485be58ffd983b8d96430efe8aed620ad0ade/Vortex.Dashboard.API/Security/DashboardAuthService.cs`
- `https://github.com/absolutezeroo/vortex-cloud/blob/afc485be58ffd983b8d96430efe8aed620ad0ade/Vortex.Dashboard.API/Security/DashboardAuthenticationHandler.cs`
- `https://github.com/absolutezeroo/vortex-cloud/blob/afc485be58ffd983b8d96430efe8aed620ad0ade/Vortex.Dashboard.API/Security/DashboardPrincipal.cs`
- `https://github.com/absolutezeroo/vortex-cloud/blob/afc485be58ffd983b8d96430efe8aed620ad0ade/Vortex.Dashboard.API/Security/DashboardSessionStore.cs`
- `https://github.com/absolutezeroo/vortex-cloud/blob/afc485be58ffd983b8d96430efe8aed620ad0ade/Vortex.Dashboard.API/Operations/DashboardOperationsService.cs`
- `https://github.com/absolutezeroo/vortex-cloud/blob/afc485be58ffd983b8d96430efe8aed620ad0ade/Vortex.Dashboard.API/Operations/DashboardOperationsService.Staff.cs`
- `https://github.com/absolutezeroo/vortex-cloud/blob/afc485be58ffd983b8d96430efe8aed620ad0ade/Vortex.Dashboard.API/Operations/StaffOperationContracts.cs`
- `https://github.com/absolutezeroo/vortex-cloud/blob/afc485be58ffd983b8d96430efe8aed620ad0ade/Vortex.Dashboard.API/Hosting/DashboardEndpoints.cs`
- `https://github.com/absolutezeroo/vortex-cloud/blob/afc485be58ffd983b8d96430efe8aed620ad0ade/Vortex.Dashboard.API/Hosting/DashboardEndpoints.Account.cs`
- `https://github.com/absolutezeroo/vortex-cloud/blob/afc485be58ffd983b8d96430efe8aed620ad0ade/Vortex.Dashboard.API/Hosting/DashboardWebHost.cs`
- `https://github.com/absolutezeroo/vortex-cloud/blob/afc485be58ffd983b8d96430efe8aed620ad0ade/Vortex.Dashboard.API/Http/DashboardAuditEmitter.cs`

## Permissions / auth

- `https://github.com/absolutezeroo/vortex-cloud/blob/afc485be58ffd983b8d96430efe8aed620ad0ade/Vortex.Primitives/Permissions/Capabilities.cs`
- `https://github.com/absolutezeroo/vortex-cloud/blob/afc485be58ffd983b8d96430efe8aed620ad0ade/Vortex.Authentication/Permissions/DefaultRoles.cs`
- `https://github.com/absolutezeroo/vortex-cloud/blob/afc485be58ffd983b8d96430efe8aed620ad0ade/Vortex.Primitives/Authentication/IAccountMfaService.cs`
- `https://github.com/absolutezeroo/vortex-cloud/blob/afc485be58ffd983b8d96430efe8aed620ad0ade/Vortex.Primitives/Authentication/IAccountAuthenticator.cs`
- `https://github.com/absolutezeroo/vortex-cloud/blob/afc485be58ffd983b8d96430efe8aed620ad0ade/Vortex.Primitives/Authentication/AccountSessionStore.cs`

## CFH

- `https://github.com/absolutezeroo/vortex-cloud/blob/afc485be58ffd983b8d96430efe8aed620ad0ade/Vortex.Dashboard.API/Api/DashboardApiService.Cfh.cs`
- `https://github.com/absolutezeroo/vortex-cloud/blob/afc485be58ffd983b8d96430efe8aed620ad0ade/Vortex.Rooms/CfhTicketService.cs`
- `https://github.com/absolutezeroo/vortex-cloud/blob/afc485be58ffd983b8d96430efe8aed620ad0ade/Vortex.Rooms/Grains/ModerationQueueGrain.cs`
- `https://github.com/absolutezeroo/vortex-cloud/blob/afc485be58ffd983b8d96430efe8aed620ad0ade/Vortex.Database/Entities/Moderation/CfhTicketEntity.cs`
- `https://github.com/absolutezeroo/vortex-cloud/blob/afc485be58ffd983b8d96430efe8aed620ad0ade/Vortex.Database/Entities/Moderation/CfhTopicEntity.cs`
- `https://github.com/absolutezeroo/vortex-cloud/blob/afc485be58ffd983b8d96430efe8aed620ad0ade/Vortex.Database/Entities/Moderation/CfhCategoryEntity.cs`
- `https://github.com/absolutezeroo/vortex-cloud/blob/afc485be58ffd983b8d96430efe8aed620ad0ade/Vortex.Dashboard.Web/src/pages/CfhQueuePage.svelte`
- `https://github.com/absolutezeroo/vortex-cloud/blob/afc485be58ffd983b8d96430efe8aed620ad0ade/Vortex.Dashboard.Web/src/pages/CfhStatsPage.svelte`

## Tests / Web

- `https://github.com/absolutezeroo/vortex-cloud/blob/afc485be58ffd983b8d96430efe8aed620ad0ade/Vortex.Dashboard.Tests/Hosting/authorization-matrix.txt`
- `https://github.com/absolutezeroo/vortex-cloud/blob/afc485be58ffd983b8d96430efe8aed620ad0ade/Vortex.Dashboard.Tests/Hosting/DashboardOperationReasonTests.cs`
- `https://github.com/absolutezeroo/vortex-cloud/blob/afc485be58ffd983b8d96430efe8aed620ad0ade/Vortex.Dashboard.Web/src/components/MfaModal.svelte`
- `https://github.com/absolutezeroo/vortex-cloud/blob/afc485be58ffd983b8d96430efe8aed620ad0ade/Vortex.Dashboard.Web/src/lib/dashboardPermissions.js`
- `https://github.com/absolutezeroo/vortex-cloud/blob/afc485be58ffd983b8d96430efe8aed620ad0ade/Vortex.Dashboard.Web/src/lib/routes.js`
