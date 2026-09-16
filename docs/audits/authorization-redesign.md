# Authorization redesign — design document

- **Status**: design. **No production code is modified by this document.**
- **Reference revision**: `62844a3` on `claude/vortex-cloud-beta-audit-apl5gn`.
- **Date**: 2026-09-15.
- **Reading prerequisite**: `docs/audits/security-audit.md` (the finding this document proposes to fix).
- **Scope**: the packet authentication boundary, and the room grain authorization boundary. Not cryptography, not web sessions, not plugins.

---

## 1. What is being fixed, in one page

The security audit found **no exploitable path** on the routes that matter. The defect is not the security level: it is that security **is not inspectable**.

> The same question — "is this actor allowed?" — gets **15 different answers, in 27 combinations, across 179 decision points**. Two of them are, when reading a diff, indistinguishable from a method that checks nothing.

```csharp
IRoomItem? item = await FindManipulableItemAsync(ctx, itemId);   // queries the SecurityModule
_state.ItemsById.TryGetValue(itemId, out IRoomItem? item);       // queries nothing
```

**The diagnosis fits in one sentence**: authorization is today *a step you remember to write*. Nothing forces the question to be asked; forgetting it means writing *less* code, never invalid code. The compiler sees nothing, the tests see nothing (you have to write the unauthorized-actor test *on purpose*, and it exists for 2 methods out of 121), and no tool can compute coverage since no list of decision points exists.

**The principle of the redesign fits in another one**:

> Turn authorization from a **remembered step** into a **declaration that the framework enforces and a script can count.**

Two gates, the same shape, at the system's two real boundaries.

---

## 2. The decisive point: both mechanisms already exist

This is what makes this redesign reasonable before a reopening rather than reckless. It introduces **no new infrastructure**: it applies two patterns your server already runs in production.

| Mechanism | Already used by | For the redesign |
|---|---|---|
| `IIncomingGrainCallFilter` | `ObservabilityGrainCallFilter` (`Vortex.Observability/Runtime/`), registered in DI | `AuthorizationGrainCallFilter` |
| `IMessageBehavior<IMessageEvent>` + `[Order(int.MinValue)]` | `RateLimitBehavior` (`Vortex.Messages/Behaviors/`) | `AuthenticationBehavior` |

The observability filter already reads `context.InterfaceMethod` — precisely what is needed to read an attribute placed on the interface method. And `RateLimitBehavior` is registered for `IMessageEvent` itself, relying on `EnableInheritanceDispatch` to cover every concrete type without enumerating them: that is the machinery the authentication gate needs, already proven on the same path.

---

## 3. Gate 1 — authentication, in the packet pipeline

### 3.1 The current state

A packet's path is `PackageHandler.HandleCoreAsync` → `MessageSystem.PublishAsync` → `MessageRegistry.PublishAsync` → handler. **None of the three refuses an unauthenticated session.** The only rampart is the `if (ctx.PlayerId <= 0) return;` that each handler writes itself: **351 out of 559** do.

That is the right behaviour obtained by the wrong method. It is repeated 351 times, absent 208 times, and nothing says which of the 208 absences is deliberate.

### 3.2 The target

```csharp
[Order(int.MinValue + 1)]   // right after RateLimitBehavior
public sealed class AuthenticationBehavior(IVortexMetrics metrics)
    : IMessageBehavior<IMessageEvent>
{
    public ValueTask InvokeAsync(
        IMessageEvent env, MessageContext ctx, Func<ValueTask> next, CancellationToken ct)
    {
        if (ctx.PlayerId > 0 || PreAuthentication.Allows(env.GetType()))
        {
            return next();
        }

        metrics.PacketDropped("unauthenticated");
        return ValueTask.CompletedTask;
    }
}
```

and, on the only handlers that run before login:

```csharp
[PreAuthentication("Establishes the session: this packet is what supplies the PlayerId.")]
public class SSOTicketMessageHandler(...) : IMessageHandler<SSOTicketMessage>
```

### 3.3 The pre-auth set is small, and that is the whole point

Eight handlers, all in `Vortex.PacketHandlers/Handshake/`, of which **seven never mention `ctx.PlayerId`** — which is mechanical proof that they do not need it:

| Handler | `ctx.PlayerId` | Role |
|---|---:|---|
| `ClientHelloMessageHandler` | 0 | handshake |
| `VersionCheckMessageHandler` | 0 | handshake |
| `InitDiffieHandshakeMessageHandler` | 0 | key exchange |
| `CompleteDiffieHandshakeMessageHandler` | 0 | key exchange |
| `UniqueIdMessageHandler` | 0 | machine identifier |
| `PongMessageHandler` | 0 | heartbeat |
| `DisconnectMessageHandler` | 0 | close |
| `SSOTicketMessageHandler` | 5 | **establishes** the session |

`InfoRetrieveMessageHandler`, which lives in the same folder, uses `ctx.PlayerId` twice: it is post-auth and must not be on the list.

An eight-line list, readable at a glance, replaces a property that is today invisible and spread over 559 files.

### 3.4 What it buys

- The **208** unguarded handlers become unreachable before login — including `RedeemVoucher`, which is the root of SEC-11.
- The **351** manual guards become redundant. They can be deleted, or left alone: they cost nothing and document intent. **Recommendation: delete them in batches, after the gate, never before.**
- A new handler is authenticated **by default**. That is the reversal that matters: today forgetting opens, tomorrow forgetting closes.

### 3.5 The risk, and how to hold it

**The real risk**: a legitimately pre-auth handler that nobody declares ⇒ nobody can log in any more. That is an *availability* risk, not a security one, and it shows up on the first login in development — not in production.

**Holding it**: a test that sends the full login sequence on an unauthenticated session and checks that it completes. If the list is incomplete, that test goes red before deployment.

---

## 4. Gate 2 — authorization, at the grain boundary

### 4.1 The rule already exists, written in black and white

```
"A handler is not a security boundary: the method is a member of a public grain interface,
  callable by anything in the cluster that can name the room (ROOMG-GATE-038).
  The grain is the boundary."
                          — Vortex.Rooms/Grains/Modules/RoomSecurityModule.cs:265
```

It is applied, and **tested for exactly two methods** (`StaffPowerGrainGateTests`). The other 119 room grain methods taking an actor rely on each author having thought of it. And a fourth one, `ApplyFurniEditAsync`, explicitly states **the opposite** (SEC-12).

The redesign does not change the rule. It makes it **structural** instead of remembered.

### 4.2 The target

On the **interface** — that is, where a diff review looks:

```csharp
public interface IRoomSettings : IGrainWithIntegerKey
{
    [RequiresRoomAuthority(RoomRequirement.Owner)]
    Task<bool> UpdateRoomSettingsAsync(PlayerId actor, RoomSettingsUpdate update, CancellationToken ct);

    [RequiresRoomAuthority(RoomRequirement.Rights)]
    Task SetRoomTagsAsync(PlayerId actor, ImmutableArray<string> tags, CancellationToken ct);
}

public interface IRoomAvatars : IGrainWithIntegerKey
{
    [NoRoomAuthority("Acts on the actor's own avatar; being in the room is enough.")]
    Task SetAvatarDanceAsync(ActionContext ctx, int danceId, CancellationToken ct);
}
```

and a filter that enforces it, on the exact model of `ObservabilityGrainCallFilter`:

```csharp
public sealed class AuthorizationGrainCallFilter(...) : IIncomingGrainCallFilter
{
    public async Task Invoke(IIncomingGrainCallContext context)
    {
        if (context.InterfaceMethod?.GetCustomAttribute<RequiresRoomAuthorityAttribute>()
            is not { } required)
        {
            await context.Invoke().ConfigureAwait(false);   // [NoRoomAuthority] or out of scope
            return;
        }

        // The actor is read from the arguments: PlayerId actor, or ActionContext ctx.
        // The convention is validated at startup (§4.3), so here it holds.
        if (!await Authority.GrantsAsync(context, required.Requirement))
        {
            throw new VortexException(VortexErrorCodeEnum.NoPermission);
        }

        await context.Invoke().ConfigureAwait(false);
    }
}
```

`RoomRequirement` is the **named** set of requirements, and its implementation is today's `RoomSecurityModule`, unchanged: `Owner` → `IsRoomOwnerAsync`, `Rights` → `CanManipulateFurniAsync`, `Capability(x)` → `HasCapabilityAsync`, `ItemOwner` → the owner comparison. **The redesign redefines no business rule** — that is the condition for it to be safe to do before a reopening.

### 4.3 The piece that changes everything: startup validation

```csharp
// At silo startup: enumerate the methods of every room grain interface that take an
// actor, and refuse to start if one declares nothing.
IReadOnlyList<MethodInfo> undeclared = RoomAuthoritySurface.FindUndeclared();

if (undeclared.Count > 0)
{
    throw new InvalidOperationException(
        "These room grain methods take an actor without declaring "
      + "[RequiresRoomAuthority] or [NoRoomAuthority]:\n  "
      + string.Join("\n  ", undeclared.Select(m => $"{m.DeclaringType!.Name}.{m.Name}")));
}
```

This is the move from:

> "nothing can compute coverage"

to:

> "coverage is total, or the process does not start".

The `check-authorization-surface.mjs` check shipped with the audit then stops being a heuristic inventory and becomes an **assertion**, verifiable offline as well as at startup.

It is also the answer to the question you left to my judgement: **refuse to start** rather than refuse the call. An undeclared method is a programming error, not a runtime event; it should cost a failed startup in development, never a silently dead feature for a player.

### 4.4 What it buys

- Both invisible idioms **disappear from the boundary**: the gate moves up onto the interface, visible in the diff.
- SEC-12 resolves by construction: `ApplyFurniEditAsync` carries `[RequiresRoomAuthority(RoomRequirement.Capability(Capabilities.Room.FurniEdit))]` and the guarantee stops depending on its caller's goodwill.
- A new room grain method **cannot** be added without someone writing, in one line, what it requires — or why it requires nothing.
- The sentence "the grain is the boundary" becomes mechanically true, and not just a comment.

---

## 5. Execution plan

Five steps, each built and tested separately, each stoppable. The order is not negotiable: gates before deletions, always.

| # | Step | Cost | What it closes |
|---|---|---|---|
| 1 | **SEC-10 + SEC-11** — per-IP and global session caps; guard + format + counting on vouchers | ~0.5 d | the two exploitable defects |
| 2 | **Gate 1** — `AuthenticationBehavior` + `[PreAuthentication]` on the 8 | ~0.5 d | the 208 unguarded handlers |
| 3 | **Gate 2** — `RoomRequirement`, attributes, filter, startup validation, **without migrating any caller** | ~1 d | the infrastructure, empty |
| 4 | **Declare the 121 methods** — one line each, by interface family | ~1.5 d | coverage goes to 100% |
| 5 | **Remove the now-redundant guards** + re-baseline the checks + refusal tests | ~1 d | the duplication, and future drift |

**Total: 4 to 5 days**, the first two of which close what is actually exploitable. If you stop after step 2, you already have most of the security gain; steps 3 to 5 buy *non-regression*, that is, the absence of future surprises.

Step 4 is the longest and the least risky: it writes no logic, only declarations, and the §4.3 validation says exactly when it is done.

---

## 6. The traps, found while writing the code

I started implementing steps 1 and 2 before withdrawing it at your request; the tree is clean. What it taught deserves to be written down, because these are the places where a follow-up will get it wrong.

**The per-IP count must be keyed on the *admission* address, not on the context.** At close time, `ISessionContext.RemoteIpAddress` may already be null: decrementing by re-reading the address from the context leaks a token per disconnect, until the cap refuses everyone. A second `SessionKey → admitted address` table is needed.

**The increment and the test must be one atomic step.** `AddOrUpdate` then compare the result, never "read, compare, increment": two connections arriving on the last token would both pass.

**A refusal must give its token back.** Otherwise a refused attacker still consumes the cap shared by everyone on their address — the refusal becomes the attack.

**`AddSessionAsync` must return its decision.** Today it returns `Task`; it needs `Task<bool>` and two callers (`SuperSocketHostBuilderExtensions`, `NetworkManager`) that close the transport on refusal. Closing from inside the gateway is shorter and less honest: the caller owns the transport.

**Voucher code length is not a setting, it is a fact.** `VoucherEntity.Code` is `[MaxLength(64)]`: a longer string cannot match any row. Refusing it **before** naming the grain therefore cannot break anything that could have worked — which is what makes the validation safe to apply to codes already in circulation. Restricting the *character set*, on the other hand, would break existing codes that cannot be enumerated without the database.

**A voucher refusal must be indistinguishable.** Answering "you are rate limited" rather than "unknown code" gives the guesser the one bit they need to pace themselves.

---

## 7. What this redesign does not solve

To be said plainly, so the document does not promise more than it holds.

- **It does not check that a requirement is the *right* one.** Declaring `[RequiresRoomAuthority(RoomRequirement.Rights)]` where `Owner` was needed passes every gate. It guarantees that a decision was **made and written down**, not that it is correct. That is a huge improvement over "no visible decision", and it is not the same thing as correctness.
- **It does not cover non-room grains.** `IPlayerGrain`, `IPlayerWalletGrain`, the catalog and marketplace grains have the same property of being callable by the whole cluster. The same attribute extends to them without changing the mechanism, but the inventory has to be redone and this document has not done it.
- **It touches neither crypto, nor web sessions, nor plugins.** The three open findings from the beta report (AUTH-01 replayable SSO ticket, SEC-01 no account lockout and sessions not revoked on ban) remain whole and are not addressed here.
- **It does not replace refusal tests.** `StaffPowerGrainGateTests` remains the right model: for each requirement, an actor who lacks it, and the assertion that nothing moved. The gate prevents the oversight; the test verifies the intent.

---

## 8. Acceptance criteria

How you know it is done, without having to take anyone's word for it.

1. `dotnet build Vortex.Cloud.sln`: 0 errors. The pre-work baseline is green (verified on `62844a3`).
2. The full suite passes, **with no disabled or quarantined test**.
3. The silo starts. If it refuses, it names the undeclared methods — and that is the expected behaviour, not an outage.
4. A client connects and plays: the full login sequence completes on an unauthenticated session (that is the test guarding the `[PreAuthentication]` list).
5. `node scripts/hooks/check-authorization-surface.mjs`: no undeclared `NONE` entry left; the number of distinct idioms has dropped — that is step 5's progress measure.
6. The (N+1)th connection from the same address is closed immediately.
7. A 200-character voucher code activates no grain, and neither does the 11th failure in a minute.
8. An actor without rights calling a protected grain method directly is refused **by the filter**, going through no handler.
