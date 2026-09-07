# Vortex Cloud — Dashboard Architecture

> The architecture contract for the Dashboard, given by the project owner, describing the shape to
> preserve for every future feature.
>
> `dashboard-architecture-rules.md` beside it is the *migration* contract: the 41 rules that governed
> dismantling the god services. That migration is complete. Where the two disagree — notably on
> administrative writes — this document is the one that holds.
>
> See also `AGENTS.md` (repository-wide contract) and `CONTEXT.md` (what each project is for).

# 1. Purpose

The Dashboard is the administration and observability interface of Vortex Cloud.

It exists to:

* inspect the state of the hotel;
* administer players;
* manage the catalogue and the economy;
* configure the progression systems;
* administer content;
* act on rooms;
* handle moderation;
* read audit and observability data;
* change some content without restarting the emulator.

The Dashboard is deliberately separated from the business runtime. It consumes the system's public
interfaces and must not become a second implementation of the hotel's rules.

# 2. General organisation

The Dashboard backend is organised around functional subjects. The main families are **Catalogue**,
**Progression**, **Hotel**, **Platform** and **Safety**.

Each subject owns the components it actually needs. A subject may expose a `*Reads` class, an
`*Operations` class, response contracts, request contracts and HTTP endpoints.

There is no longer a global service carrying every read or every operation. That separation is what
lets each feature declare its dependencies explicitly.

# 3. The shape of a feature

A Dashboard feature generally follows this path:

```
HTTP endpoint
  → the subject's Reads or Operations
  → business service, grain or admin service
  → runtime / database / provider
  → typed response
```

The HTTP endpoint stays an adaptation layer. It owns the route, deserialisation, HTTP validation,
authorisation, the call into the service, and the final translation to an HTTP response.

It does not carry the domain's business rules.

# 4. Reads

Dashboard reads live in `*Reads` classes — `CatalogReads`, `DirectoryReads`, `EconomyReads`,
`PollReads`, `RewardTrackReads`, `NavigatorReads`, `BenchmarkReads`, and so on.

A read class declares only the dependencies its subject needs. Reads may go to the database directly
when they build a consultation model.

**They never modify state.**

# 5. DashboardReads

Read classes may inherit from `DashboardReads`. That base has a deliberately minimal
responsibility: open a database context, run the read, dispose the context correctly.

It must not become a home for business rules, a domain's helpers, permissions, business pagination,
Orleans dependencies, or transformation logic belonging to one feature.

Specific dependencies stay visible in each read class's own constructor.

# 6. API contracts

Every public read has a named response shape. Responses no longer rest on anonymous objects exposed
as `object`. The contract is an explicit part of the architecture.

The type chain is:

```
C# response contract
  → OpenAPI schema
  → generated TypeScript definition
  → API client
  → Svelte page or component
```

The goal is that a breaking backend change is caught when the frontend compiles rather than when a
user opens the page.

# 7. Contracts and the domain

An HTTP contract is not automatically a domain type.

When a response is a projection built specifically for the Dashboard, it gets its own contract.

When a runtime type already represents exactly the data being sent, it may be reused so that one
structure is not described twice. Duplicating identical models is to be avoided.

Reuse must not, however, couple the Habbo client protocol to the administrative contracts.

# 8. TypeScript frontend

The Dashboard frontend is entirely TypeScript. Every Svelte component is typed, and so are the
shared helpers.

The frontend must not hand-write a contract the backend already publishes. The generated types are
the reference for anything coming from the API.

Local types stay appropriate for form state, component state, a projection built purely for the
interface, a union describing a UI state, or a deliberate transformation of a network DTO.

# 9. Operations

Mutations are grouped in `*Operations` classes.

An Operations class does not reimplement business rules. It orchestrates one administrative action
from the operator's context, the request, the business service concerned, and the Dashboard's
cross-cutting infrastructure.

Operations go through `OperationRunner`.

# 10. OperationRunner

`OperationRunner` is the cross-cutting pipeline for administrative mutations. It centralises the
correlation id, tracing, execution time, metrics, entity change capture, audit, the distinction
between an expected refusal and a technical error, and protection against leaking internal errors.

A feature must not copy that logic. A feature receives `OperationRunner` as a collaborator.

`OperationRunner` must not carry business logic belonging to a domain.

# 11. Write ownership

Not every mutation needs the same mechanism. The important question is not "does the Dashboard write
to the database directly?" but:

**"who owns the mutable state concerned?"**

Four categories must be distinguished.

**A. Persistent data re-read on every use.** A direct write is acceptable when no live state keeps a
durable copy. Consistency comes naturally from the next read.

**B. State owned by a grain.** When the grain is the authority over the mutable state, the mutation
goes through that grain. The Dashboard must not change the row underneath an active grain without
telling it.

**C. Persistent data feeding a cache or provider.** A database write may be followed by an explicit
reload or invalidation. The change is only consistent once the corresponding live state has been
refreshed.

**D. State owned in memory with no synchronisation mechanism.** A direct database write is
forbidden. It would create two truths: the database, and the state the runtime holds.

# 12. The rule for mutations

An administrative mutation must never directly modify state that another part of the runtime is the
live authority over, unless an explicit and reliable synchronisation mechanism exists.

That is more precise than forbidding every use of `SaveChangesAsync` in the Dashboard.

Authoring services are allowed to manage their own data directly when that data genuinely belongs to
them.

# 13. Admin services

`*AdminService` classes are the authoring and administration operations that do not necessarily map
to an ordinary runtime action by a player — catalogue editing, furniture definitions, reward
editing, progression content, poll configuration, Navigator management.

They may validate content, write to the database, trigger a reload, invalidate a cache, notify a
grain, or refuse a dangerous operation.

They must not bypass an existing runtime owner.

# 14. Live consistency

An administrative operation succeeds *technically* when its change is persisted. It succeeds
*functionally* when the hotel observes that change too.

A write may therefore need no further work, a grain call, a reload, an invalidation, a notification,
or a future reactivation of the context concerned.

Admin services must document that obligation where it exists.

# 15. Active rooms

Some data is copied into a room's state when the room is materialised. Changing the source
definition does not necessarily change objects already materialised.

Reference data and the snapshots held by currently active rooms must be told apart, and that
distinction documented for any configuration that behaves this way.

# 16. Audit

Every significant administrative operation must produce an audit entry, carrying the action, the
operator, the account actually authenticated, the reason, the target, the correlation id, the result,
and the captured changes where available.

The identity supplied by the caller is not the only source of truth. The server's security context
stays authoritative.

# 17. Mandatory reason

Sensitive mutations require a reason from the operator. That reason is part of the operation, is
validated server-side, and is recorded in the audit.

The frontend may help the user provide it, but never constitutes the security validation.

# 18. Permissions

Permissions are checked server-side. Whether a button is visible or enabled in the frontend is an
interface feature and never a security mechanism.

Routes declare the capability they need. The frontend uses the same capabilities to shape the
interface.

# 19. MFA and step-up

Some sensitive operations may need an extra check. The Dashboard has a step-up mechanism that asks
for a second proof of identity before performing the operation.

That policy stays independent of the business feature.

# 20. Error handling

An unexpected error must not expose an EF message, a SQL query, a stack trace, or any internal
detail. The client receives a generic error and a correlation id.

Business refusals use stable codes meant for the application. They must not depend on a sentence
written for a human.

# 21. Directory and investigation

The Directory domain is a cross-cutting read model for administrative investigation. It may
aggregate several sources to provide profiles, histories, events, room information, economic
information and audit data.

That cross-cutting reach is acceptable *because it is a read model*. It must not turn into a
cross-cutting mutation layer.

# 22. Endpoints

`DashboardEndpoints` stays physically organised as partials. That is acceptable because the class is
stateless, has no global dependencies, delegates to the subjects' services, and mainly serves as a
route registry.

The partials must never become a way to hide a global business service again.

# 23. Hosting

The Dashboard runs in a dedicated ASP.NET Core application inside the emulator process, with its own
HTTP pipeline. The services it needs are supplied from the main container.

That isolation is what keeps the Dashboard optional. A Dashboard failure must not necessarily stop
the hotel; the behaviour depends on its `required` configuration.

# 24. Resilience

The Dashboard must be able to be disabled. Its unavailability must not prevent the main runtime from
running, except where the operator has explicitly configured the Dashboard as a required component.

That property must be preserved through any future change to the hosting.

# 25. Tests

Dashboard tests cover authorisation, capabilities, MFA step-up, mandatory reasons, response schemas,
TypeScript contract generation, business errors, the HTTP pipeline, audit, and some architecture
rules.

An important architectural rule should ideally become executable. An executable rule must, however,
represent the real architecture exactly.

**An incorrect architectural test is more dangerous than a missing one, because it pushes the code
towards a bad architecture.**

# 26. Fundamental rules

1. A domain owns its dependencies.
2. A feature does not depend on a global Dashboard service.
3. Reads do not modify state.
4. Operations do not reimplement the business.
5. Writes respect the real owner of the state.
6. HTTP contracts are explicitly typed.
7. The frontend consumes types coming from the backend.
8. Security stays server-side.
9. Audit stays cross-cutting and uniform.
10. A cache or a grain must never be left inconsistent after an administrative write.
11. Shared classes must not accumulate business logic.
12. A folder or a partial is not on its own an architectural boundary.

# 27. Quality criterion

The main question to ask of a Dashboard feature is:

**"Can this feature be understood, tested, changed or removed without understanding the whole
Dashboard?"**

If the answer is yes, the boundary is probably right.

If a change requires touching several global components for no business reason, the feature probably
lacks autonomy.

# 28. Current target architecture

**Frontend**

```
Svelte TypeScript
  → typed API client
  → generated TypeScript contracts
```

**HTTP**

```
Typed endpoint
  → capability / validation
  → Reads or Operations
```

**Read**

```
Reads
  → DB / runtime projection
  → response contract
```

**Mutation**

```
Operations
  → OperationRunner
  → business service / grain / AdminService
  → persistence and live synchronisation
  → audit / metrics / result
```

This is the architecture to preserve for future Dashboard features.

---

## What is executable today

Rules that no compiler enforces are worth encoding — but only where the check matches the
architecture exactly (§25).

| Check | What it holds |
|---|---|
| `ApiTypeScriptContractTests` | §6/§8 — `apiTypes.d.ts` is generated from the C# contracts and fails on drift |
| `DashboardResponseSchemaTests` | §6 — a converted read must publish its response schema |
| `DashboardEndpointServiceTests` | §23 — an endpoint injecting a service the child container cannot resolve |
| `check-dashboard-capabilities.mjs` | §18 — a capability declared in three of its four places |
| `check-architecture-walls.mjs` wall 2 | §26.3 — persistence lives in `Admin/`; reads never write |

Wall 2 is deliberately narrower than §11-12. Ownership of live state is not something a regex can
see, so the mechanical part is guarded and the ownership rule stays prose.

The earlier version of that wall forbade `SaveChangesAsync` anywhere in `Vortex.Dashboard.API` and
reported 146 violations — every one an authoring service doing exactly what §13 says it may do. That
rule was never in the migration contract either; it was invented in the hook's own comment, next to
a claim that all seven walls held. It was replaced for the reason §25 gives: a wall that fails on
correct code teaches everyone to push with `--no-verify`, and the next real regression goes through
with it.
