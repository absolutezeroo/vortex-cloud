# Dashboard C# — Architecture Rules

> Architecture contract for `Vortex.Dashboard.API`, given by the project owner.
> It outranks case-by-case judgement: an extraction that breaks one of these rules is to be redone,
> even if it compiles and the tests pass.
>
> See also `AGENTS.md` (repository-wide contract) and `CONTEXT.md` (what each project is for).

## Goal

The Dashboard must stay a **clean, modular, maintainable administration module**, without becoming a
central point that the rest of the emulator ends up coupled to.

The goal is not merely tidy files.

The architecture must make it so that:

* every domain is clearly identifiable;
* every class has a limited responsibility;
* every dependency is justified;
* domains stay independent of one another;
* cross-cutting concerns stay centralised;
* HTTP contracts stay stable;
* security rules stay enforced server-side;
* operations stay audited;
* changes stay testable;
* adding a feature does not require editing a pile of global files.

---

# 1. Main rule: a domain must be a real unit

Every functional subject of the Dashboard must progressively become autonomous.

Subjects, for example:

* Catalog
* Quests
* Reward Tracks
* Habbicons
* Polls
* Moderation
* Rooms
* Navigator
* Articles
* Fishing
* Furniture
* Mystery Box
* Prize Pools
* Staff
* Backup
* Benchmark
* Console
* Economy

A domain must not be merely one file belonging to an enormous partial class.

It must own its responsibilities and its dependencies.

## Good

* A domain owns its reads.
* A domain owns its operations.
* Its dependencies are directly visible.
* Its endpoints call the domain's own service directly.
* The domain does not know about services it does not need.
* Changes inside a domain have little impact on the others.

## Bad

* One single service holding every Dashboard feature.
* A giant class merely split into `partial` files.
* Adding a dependency for one feature and thereby handing it to thirty features that never use it.
* Believing a folder split is enough to make an architecture modular.
* Creating a new God Service under a different name.

---

# 2. The old God Services must progressively disappear

`DashboardApiService` and `DashboardOperationsService` are to be treated as temporary structures for
the duration of the migration.

The end goal is not to make them smaller.

The end goal is that they have no reason left to exist.

On every domain extraction:

* move the feature concerned;
* move only the dependencies genuinely needed;
* **remove the old dependencies from the God Service**;
* remove the fields that became useless;
* remove the imports that became useless;
* update the endpoints concerned;
* run the tests;
* verify external behaviour did not change.

## Good

The method and dependency counts of the old services go down on every migration.

## Bad

Creating new per-domain classes while leaving every old dependency in the God Services.

That would only look like a split architecture.

---

# 3. A class must declare its real dependencies

A class's constructor must state precisely what that class needs in order to work.

A Catalog feature must not receive dependencies belonging to Polls, Fishing, Moderation or Backup.

A large number of constructor dependencies is to be read as an architectural warning sign.

## Good

* Few dependencies.
* Dependencies coherent with the domain.
* Dependencies easy to explain.
* Dependencies directly visible.
* Specialised services.

## Bad

* Dozens of dependencies.
* A class that indirectly depends on nearly the whole Dashboard.
* Injecting a service merely because it is already available.
* Keeping a dependency after moving the feature that used it.

---

# 4. Cross-cutting policy must stay centralised

Some responsibilities are common to every administrative operation.

They must never be re-implemented per domain.

Namely:

* audit;
* correlation ID;
* tracing;
* metrics;
* change capture;
* uniform error handling;
* translation of business rejections;
* failure logging.

`OperationRunner` is that shared policy.

That responsibility stays centralised.

## Good

Business features ask the shared mechanism to run a controlled operation.

## Bad

* Copying audit logic into several services.
* Building a different audit system per domain.
* Letting a domain bypass the standard operation mechanism.
* Using inheritance to force domains into being variants of the audit mechanism.
* Mixing cross-cutting policy with business logic.

---

# 5. Prefer composition for important behaviour

A feature must not inherit from a huge base carrying security, audit, metrics or business behaviour.

Important policy must be supplied as an explicit collaborator.

Inheritance stays exceptional and minimal.

## Acceptable

An extremely small, purely technical base may be used when it carries no business rule and no policy.

## Bad

Growing a base class that ends up holding:

* pagination;
* permissions;
* user lookup;
* asset generation;
* Orleans access;
* business validation;
* metrics;
* audit;
* parsing;
* domain-specific logic.

Such a base would simply be a new God Service reached by inheritance.

---

# 6. The read layer stays distinct from the write layer

Reads and administrative operations answer to different constraints.

Reads:

* fetch data;
* assemble read models;
* may go straight to the data sources meant for that;
* must not mutate business state.

Operations:

* mutate state;
* go through the appropriate business services or grains;
* are controlled;
* are audited;
* must respect the domain's rules.

## Good

A clear separation between reads and writes.

## Bad

* Writing to the database from a read.
* Doing arbitrary EF writes from endpoints.
* Bypassing the domain's services to save a few lines.
* Mixing reporting queries and administrative commands in one giant class.

---

# 7. Operations go through the existing domain APIs

The Dashboard is an administration surface for the emulator.

It must not become a second implementation of the business.

When a domain already has:

* an admin service;
* a business abstraction;
* a grain;
* a domain API;

the Dashboard uses that abstraction.

## Good

The Dashboard orchestrates the domain's features.

## Bad

The Dashboard re-creates business rules in its own services.

That eventually produces two different implementations of the same rule.

---

# 8. A shared responsibility lives with its true owner

When several features need the same behaviour, identify its real responsibility before creating an
abstraction.

An asset-URL concern belongs to the asset infrastructure.

An audit concern belongs to the operation system.

A Catalog rule stays in Catalog.

## Good

Move the behaviour to the component that actually owns that responsibility.

## Bad

* Copying a method.
* Creating a common base for two classes purely to share one method.
* Dropping every helper into a `Utils` folder.
* Creating generic classes whose name maps to no real responsibility.

---

# 9. Avoid the "Utils" folder or class

A large `Utils`, `Helpers`, `Common` or `Misc` generally becomes the next catch-all.

A shared function must have a conceptual owner.

## Good

Cross-cutting components have a precise name matching their responsibility.

## Bad

Using `Utils` as the default destination whenever it is unclear where something goes.

---

# 10. HTTP contracts must become explicit

The Dashboard's important responses must progressively get dedicated types.

Anonymous types are convenient for small local responses, but they become a problem when:

* the response is important;
* it holds many sections;
* several consumers use it;
* it has to evolve;
* it has to appear in OpenAPI;
* the frontend depends heavily on its shape.

## Good

The main read models and public contracts are explicitly defined.

## Bad

Building enormous responses out of dozens of nested anonymous types.

---

# 11. The API contract must become the frontend's source of truth

Eventually, the models the backend exposes must be able to feed the frontend types automatically via
OpenAPI.

The point is to stop backend and frontend each describing the same structure by hand.

## Good

* Explicit backend contracts.
* Trustworthy OpenAPI.
* Generated or derived TypeScript types.
* A breaking change caught early.

## Bad

* Anonymous C# response.
* Untyped JavaScript frontend.
* Manual documentation.
* Implicit assumptions about properties.
* Errors discovered only at runtime.

---

# 12. An endpoint answers its consumer's real need

A small component must not call an enormous endpoint just because the information it needs happens to
be somewhere in the response.

Every read model is sized for its use.

## Good

Specialised reads when the needs genuinely differ.

## Bad

A universal endpoint that systematically fetches:

* profile;
* audit;
* economy;
* history;
* messages;
* inventory;
* rooms;
* statistics;

when the consumer uses a handful of fields.

---

# 13. Do not compute data nobody uses

Every field an API returns has a potential cost:

* SQL query;
* allocation;
* serialisation;
* transfer;
* maintenance;
* one more contract.

Information nobody uses must not be computed "just in case".

## Good

Measure actual usage.

Delete the useless computation.

## Bad

Adding counters, histories or aggregates with no real consumer.

---

# 14. Ask abstractions the right question

An abstraction must expose the question its consumer actually has.

If the need is to know whether a user is online, the API must answer that question directly.

If the need is only the number of connected users, do not build the whole user list.

## Good

Intentional, efficient APIs.

## Bad

Exposing only large primitive collections and leaving every consumer to rebuild its own answer.

---

# 15. Avoid needless allocations on hot paths

Frequently used paths must avoid:

* collection copies;
* pointless full materialisation;
* O(n) scans when an indexed structure exists;
* counting by first building a list;
* extra, useless SQL queries.

Metrics and presence information matter especially here, because they can be called very often.

---

# 16. Large read models must be decomposed

A method several hundred lines long is a warning sign even when it only reads.

Its sections must be identified conceptually.

## Good

A readable orchestration assembling several clearly named parts.

## Bad

One gigantic method running a dozen queries back to back with no conceptual boundaries.

---

# 17. Do not split a method artificially just to lower its line count

The point is not to turn a 600-line method into twenty arbitrary private ones.

Every extraction must map to an identifiable responsibility.

## Good

Extract a profile, a history, a timeline, an investigation section, a coherent projection.

## Bad

Creating methods named only after their execution order or their size.

---

# 18. Folders must reflect real responsibilities

The layout must make the system easier to understand.

The current top-level families can stay useful:

* Catalogue;
* Progression;
* Hotel;
* Platform;
* Safety.

But they must not hide the real domains.

## Good

A structure where a developer quickly knows where to look for a feature.

## Bad

An architecture that is physically tidy but still logically three enormous partial classes.

---

# 19. `partial` is not an architecture boundary

Partial classes are a source-organisation tool.

They create:

* no isolation;
* no dependency limit;
* no contract;
* no runtime modularity.

## Rule

A God Service split into twenty `partial` files is still a God Service.

---

# 20. Endpoints stay thin

HTTP endpoints mainly handle:

* routing;
* binding;
* authentication and authorisation;
* HTTP validation;
* the call to the right service;
* translating the response.

They must not hold the main functional logic.

## Bad

Moving logic out of the God Services only to drop it into the endpoints.

---

# 21. Security stays server-side

The frontend may hide features based on the user's capabilities, but that logic is never a security
barrier.

Every sensitive endpoint keeps enforcing its rules server-side.

Refactoring must never weaken:

* capability checks;
* read restrictions;
* audit rules;
* reason requirements;
* MFA protections;
* step-up rules;
* request validation.

---

# 22. Permissions follow the sensitivity of the data

Where an endpoint sits in the folder tree must not automatically decide its permission.

A player-related endpoint can expose sensitive data such as chatlogs.

It then keeps a capability matching the real sensitivity of what it returns.

## Good

Permissions set by the data exposed.

## Bad

Moving a route into a broader category and accidentally widening access to sensitive data.

---

# 23. Audit must never be weakened during a refactoring

An administrative operation must keep producing a usable trace.

Refactoring must preserve:

* the actor;
* the real security context;
* the reason;
* the outcome;
* the correlation ID;
* the target;
* the captured changes;
* the metrics;
* the error behaviour.

No architectural extraction justifies losing any of these.

---

# 24. Tests are the migration's safety net

Every extraction must preserve existing behaviour before introducing functional change.

Separating:

1. architectural refactoring;
2. optimisation;
3. functional change;

makes the cause of a regression immediately clear.

## Good

* Tests before a large extraction.
* Tests unchanged where behaviour must stay identical.
* Authorisation tests.
* Audit tests.
* Validation tests.
* Error-path tests.
* Tests on the important contracts.

## Bad

Doing all at once:

* renaming;
* API change;
* business-rule change;
* optimisation;
* permission change.

---

# 25. A performance change must answer a measured need

Optimisations are worth it when they remove a real cost:

* useless SQL queries;
* large allocations;
* over-broad queries;
* an oversized endpoint;
* a fully materialised collection used to produce one boolean.

## Bad

Complicating the architecture for a hypothetical optimisation with no observable benefit.

---

# 26. Admin services belong to the Dashboard when they only serve administration

Services that edit the system's content need not belong to the emulator's runtime modules.

There must be a clear distinction between:

* running the hotel normally;
* administering or authoring its content.

The Dashboard may own the services strictly meant for administration, as long as they go through the
appropriate domain contracts.

---

# 27. The Dashboard must not contaminate the runtime domains

The emulator's domains must not start depending on `Vortex.Dashboard.API`.

The direction of dependency stays under control.

The Dashboard may use the domain's public interfaces.

The domain must not know about the Dashboard.

---

# 28. The Dashboard stays optional

A Dashboard failure must not automatically take the emulator down when the configuration allows
running without a Dashboard.

That resilience property must survive hosting and DI changes.

---

# 29. The second DI container is known debt

The current forwarding mechanism between the main container and the Dashboard's is fragile.

It may stay during the domain migration, because the tests protect against omissions.

It must not, however, become the permanent answer with dozens or hundreds of hand-declared types.

## Not now

Reworking all at once:

* every service;
* the endpoints;
* the contracts;
* the hosting;
* the DI system.

## Later

Deal with forwarding and hosting once the functional boundaries have settled.

---

# 30. A migration must reduce overall complexity

After each step, ask:

* Did the total dependency count go down?
* Is the domain more autonomous?
* Are the responsibilities easier to explain?
* Did the number of global files to edit for a feature go down?
* Are the tests still stable?
* Is the HTTP contract still honoured?
* Would a similar new feature be easier to add?

If the answer is no, the refactoring has probably just moved the problem.

---

# 31. Never re-create a catch-all

During the migration, watch the new cross-cutting components especially closely.

A shared component keeps a very narrow responsibility.

Watch:

* `DashboardReads`
* `OperationRunner`
* `DashboardAssetUrls`
* any future `Common` service
* any future global helper

## Rule

The moment a shared component starts accumulating features specific to several domains, stop and
redistribute the responsibilities.

---

# 32. Names express the role

Prefer names that state the responsibility precisely.

Generic words such as:

* Manager
* Helper
* Processor
* Handler
* Service
* Utils

do not, on their own, describe a responsibility.

The name must convey:

* what the component handles;
* whether it reads or writes;
* which domain it represents.

---

# 33. A feature must not import another feature without a real business reason

Dashboard domains stay loosely coupled.

If Catalog starts depending on Quests only to fetch a small technical value, that value probably needs
a different, properly owned abstraction.

Cross-feature dependencies stay exceptional and justified.

---

# 34. Avoid premature abstraction

Two domains having a similar method does not immediately mean a shared abstraction should exist.

Wait until the genuinely common responsibility is understood.

## Good

Factoring out a clearly identified policy or infrastructure.

## Bad

Creating complex generic interfaces purely to delete a few duplicated lines.

---

# 35. Temporary duplication can beat a bad abstraction

A few similar lines in two domains are sometimes less dangerous than an ill-defined global
abstraction that couples them permanently.

Factoring must improve understanding, not just the line count.

---

# 36. Each domain must be able to evolve without changes conceptually tied to the others

A Catalog change must not systematically require editing:

* Moderation;
* Quests;
* Polls;
* Rooms;
* Articles.

The global configuration points must progressively shrink.

---

# 37. Migration statistics must be tracked

While the God Services are being dismantled, track at least:

* methods left in `DashboardApiService`;
* methods left in `DashboardOperationsService`;
* dependency count of each constructor;
* domains still implemented as partial parts;
* services that have their own classes;
* hand-written dependencies in the DI forwarding.

These numbers say whether the debt is actually going down.

---

# 38. Recommended migration order

Start with simple, clearly bounded features.

Continue with the more complex domains.

Keep for last the domains with many runtime dependencies or several crossed responsibilities.

Hosting and DI forwarding come after the business boundaries have settled.

---

# 39. What must be preserved

The parts that are currently solid must not be sacrificed to the rework:

* durable audit;
* correlation IDs;
* tracing;
* metrics;
* capability checks;
* MFA step-up;
* mandatory reasons;
* business rejection rules;
* change capture;
* authorisation tests;
* hosting tests;
* validation tests;
* the Dashboard failure-isolation mechanism;
* use of the domain interfaces;
* the separation between runtime and authoring.

---

# 40. What must progressively disappear

* God Services.
* Partial classes whose only purpose is to hide their size.
* Constructors with dozens of dependencies.
* Methods hundreds of lines long.
* Gigantic anonymous API responses.
* Universal endpoints returning far more data than needed.
* Needless collection allocations.
* SQL queries whose results are never consumed.
* Gigantic hand-written registries.
* Helpers with no clear responsibility.
* Dead dependencies.
* Business logic duplicated in the Dashboard.
* Coupling between features for no reason.
* `Common`, `Utils` and `Helpers` catch-alls.

---

# 41. Definition of the target architecture

The C# Dashboard counts as correctly restructured when:

* the main features own their classes;
* both old God Services are gone;
* dependencies are local to their domains;
* every operation goes through the same cross-cutting policy;
* reads are clearly identifiable;
* the important contracts are typed;
* endpoints stay thin;
* permissions stay enforced server-side;
* the runtime domains do not depend on the Dashboard;
* the Dashboard does not re-implement the business;
* security, audit and observability are preserved;
* adding a feature no longer means editing a multitude of global files;
* tests protect the critical boundaries;
* the physical architecture actually matches the logical one.

---

# Final principle

The main criterion is not:

> "Is the project tidy?"

The criterion is:

> "Can a feature be understood, tested, changed and deleted with minimal impact on the other
> features?"

A large Dashboard must be a set of coherent domains sharing a few clearly defined cross-cutting
infrastructures — not one global application artificially divided into files.

---

# Migration tracking (§37)

Recorded at each step, so the debt is measured instead of felt. Re-take the measurement with
`node scripts/dashboard-migration-stats.mjs`.

**Both god services are gone as of 2026-09-07.** Forty-three subjects own their classes; the ten
and twenty-nine constructor dependencies the two services held are now held by whichever subject
uses each one.

**Folder equals namespace under `Api` and `Operations`** since the same day — the partial classes
that forced them apart are gone. `Hosting` still declares one namespace across its five family
folders, because `DashboardEndpoints` is still one partial class, which §19 tolerates for thin
routing that holds no dependencies of its own.

**§29 settled the same day.** The forwarding list no longer grows with the subjects: the sixty
entries that did are derived from what the classes are (a read derives `DashboardReads`, a write
takes an `OperationRunner`), leaving eleven declared for the services endpoints inject directly.
The last column below therefore stops moving — 71 types, 11 of them written down.

Every row below comes from that script, including the baseline, which was re-measured in a worktree
at `7cd62e01d^`. Hand counts are not comparable to each other and were discarded.

| step | `DashboardApiService` | `DashboardOperationsService` | domains extracted | DI forwarding |
| --- | --- | --- | --- | --- |
| before migration | 101 methods, 10 deps | 184 methods, 29 deps | 0 | 13 types |
| catalogue extracted | 96 methods, 10 deps | 175 methods, 28 deps | 1 (Catalog) | 15 types |
| polls extracted | 92 methods, 10 deps | 169 methods, 27 deps | 2 (+ Polls) | 17 types |
| quest content + articles | 86 methods, 10 deps | 156 methods, 25 deps | 4 (+ QuestContent, Articles) | 21 types |
| songs | 85 methods, 10 deps | 152 methods, 24 deps | 5 (+ Songs) | 23 types |
| fishing | 83 methods, 10 deps | 139 methods, 23 deps | 6 (+ Fishing) | 25 types |
| navigator | 82 methods, 10 deps | 126 methods, 22 deps | 7 (+ Navigator) | 27 types |
| furniture | 81 methods, 10 deps | 123 methods, 21 deps | 8 (+ Furniture) | 29 types |
| quests | 77 methods, 10 deps | 120 methods, 20 deps | 9 (+ Quests) | 31 types |
| targeted offers | 72 methods, 10 deps | 114 methods, 19 deps | 10 (+ TargetedOffers) | 33 types |
| prize pools + mystery boxes | 67 methods, 10 deps | 98 methods, 17 deps | 12 (+ PrizePools, MysteryBox) | 37 types |
| staff | 65 methods, 10 deps | 88 methods, 15 deps | 13 (+ Staff) | 39 types |
| content | 65 methods, 10 deps | 54 methods, 14 deps | 14 (+ Content) | 40 types |
| reward tracks + habbicons | 58 methods, 10 deps | 34 methods, 11 deps | 16 (+ RewardTracks, Habbicons) | 43 types |
| gamedata | 55 methods, 9 deps | 29 methods, 10 deps | 17 (+ Gamedata) | 45 types |
| benchmark | 53 methods, 7 deps | 27 methods, 9 deps | 18 (+ Benchmark) | 47 types |
| backup, console, privacy | 53 methods, 7 deps | 22 methods, 5 deps | 21 (+ Backup, Console, Privacy) | 50 types |
| moderation | 53 methods, 7 deps | 12 methods, 5 deps | 22 (+ Moderation) | 51 types |
| rooms | 53 methods, 7 deps | 8 methods, 3 deps | 23 (+ Rooms) | 52 types |
| **currency, vouchers, config — write service deleted** | 52 methods, 7 deps | **gone** | 26 (+ Currency, Vouchers, Config) | 55 types |
| bots, groups, pets, social, wired | 44 methods, 7 deps | — | 31 | 60 types |
| audit, cfh, chatlogs, collectibles, player rewards, inventory, catalog purchases | 35 methods, 7 deps | — | 38 | 67 types |
| economy | 26 methods, 7 deps | — | 39 | 68 types |
| **achievements, resolutions, signal directories, directory — read service deleted** | **gone** | **gone** | 43 | 71 types |

The DI forwarding column going **up** while the rest goes down is the §29 debt being paid in
instalments: each extracted domain adds its two classes to a hand-written list. It is expected to
keep rising until the boundaries settle, and is the reason §29 says not to leave it that way.
