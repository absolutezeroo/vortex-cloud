# Pattern: Vertical Slice (Handler + Grain + Test, one feature)

The three samples in this folder — `HandlerPattern.cs`, `ServicePattern.cs`,
`UnitTestPattern.cs` — each show the *shape* of one layer in isolation. That answers
"what does a handler look like?" but not the harder question: **how do these layers
collaborate on a single feature?**

This vertical slice shows all three layers for **one** feature: the "wave" action from
`docs/walkthroughs/add-a-feature.md`. Read them together, top to bottom, as one unit.
The point is the seams between them, not any single file.

> These are reference-only samples in the `Docs.Patterns` namespace. They mirror real
> shapes in the codebase but are not compiled into the emulator. Copy and adapt; do not
> reference them from real code.

---

## Layer 1 — Handler (orchestration only)

The handler's entire job is: validate context, resolve the grain, delegate. It is the
boundary between "untrusted client request" and "trusted domain operation." Note the
seam: it hands off via the **grain interface** (`IRoomGrain.WaveFromPlayerAsync`) and
knows nothing about how the wave is implemented.

```csharp
using System.Threading;
using System.Threading.Tasks;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Room.Engine;

namespace Docs.Patterns;

// Reference-only: vertical slice 1/3 — packet handler.
public sealed class WaveHandlerSample(IGrainFactory grainFactory)
    : IMessageHandler<WaveMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        WaveMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        // Guard: context is the trust boundary. PlayerId/RoomId come from the
        // authenticated session, never from the packet body.
        if (ctx is null || ctx.PlayerId <= 0 || ctx.RoomId <= 0)
            return;

        // Seam: resolve the grain by its INTERFACE and delegate. The handler does
        // not know (or care) that the implementation lives in RoomAvatarModule.
        var room = _grainFactory.GetRoomGrain(ctx.RoomId);
        await room.WaveFromPlayerAsync(ctx.PlayerId).ConfigureAwait(false);
    }
}
```

## Layer 2 — Domain logic (the grain module)

The behavior lives behind the grain interface. This is where state resolution and any
permission checks happen, and where the broadcast is published. The seam *below* this
layer is `SendComposerToRoomAsync`, which hands the result to the Orleans stream — the
module never touches sessions or sockets.

```csharp
using System.Threading.Tasks;
using Turbo.Primitives.Messages.Outgoing.Room.Engine;
using Turbo.Primitives.Players;

namespace Docs.Patterns;

// Reference-only: vertical slice 2/3 — the operation behind IRoomGrain.
// In real code this is a method on RoomAvatarModule; the extracted rule below
// (WaveRule) is what the test actually targets.
public static class WaveRule
{
    /// <summary>
    /// Pure decision: can this player wave right now? Extracted from the module so it
    /// is unit-testable without spinning up a grain. The module calls this, then
    /// broadcasts only if it returns true.
    /// </summary>
    public static bool CanWave(bool playerHasAvatarInRoom) => playerHasAvatarInRoom;
}

public sealed class WaveModuleSample
{
    // Mirrors how RoomAvatarModule resolves player -> room object, then publishes.
    public async Task WaveFromPlayerAsync(
        PlayerId playerId,
        IRoomStateView state,
        IRoomBroadcast broadcast
    )
    {
        // Resolve player -> room object (the same two-step lookup RoomChatSystem uses).
        if (
            !state.TryGetObjectIdByPlayer(playerId, out var objectId)
            || !state.TryGetAvatar(objectId, out var avatar)
        )
            return;

        if (!WaveRule.CanWave(playerHasAvatarInRoom: true))
            return;

        // Seam: hand off to the room broadcast (-> Orleans stream -> presence grains).
        // The module never enumerates sessions.
        await broadcast.SendComposerToRoomAsync(new WaveComposer { ObjectId = avatar.ObjectId });
    }
}
```

> **Why `WaveRule` is extracted.** Grain modules are awkward to unit-test because they
> hold a back-reference to a live grain and its state. The repeatable trick — and the
> one `UnitTestPattern.cs` already demonstrates with `PresenceRule` — is to pull the
> *decision* out into a pure function and test that exhaustively. The module becomes a
> thin "resolve → decide → broadcast" shell, and the interesting logic is covered.

## Layer 3 — Test (failure path first)

Test the rule's branches directly. The highest-value case is the **failure path** — a
player with no avatar must not broadcast — because that is the branch a future refactor
of avatar storage is most likely to break.

```csharp
using FluentAssertions;
using Xunit;

namespace Docs.Patterns;

// Reference-only: vertical slice 3/3 — failure-path-first unit test.
public class WaveRuleTests
{
    [Theory]
    [InlineData(false, false)] // no avatar in room -> cannot wave  (the regression guard)
    [InlineData(true, true)]   // present in room   -> can wave
    public void CanWave_DependsOnPresence(bool hasAvatar, bool expected)
    {
        WaveRule.CanWave(hasAvatar).Should().Be(expected);
    }
}
```

---

## Reading the slice as one story

```
client sends WaveMessage
        │
        ▼
WaveHandlerSample        ── validates ctx, resolves IRoomGrain, delegates ──┐
        │                                                                   │  seam: grain interface
        ▼                                                                   │
WaveModuleSample         ── resolves player→object, asks WaveRule, broadcasts┘
        │                                                                   
        │  seam: SendComposerToRoomAsync → Orleans stream                    
        ▼                                                                   
(presence grains fan out to sessions — out of this slice's scope)           

WaveRule                 ── the one pure decision, tested in isolation ──────► WaveRuleTests
```

Each arrow is a **seam** — a place where one layer hands off to the next through a
narrow contract:

| Seam | Contract | Why it's a clean boundary |
|---|---|---|
| handler → grain | `IRoomGrain` interface | handler can't see implementation; grain can't see the socket |
| module → broadcast | `SendComposerToRoomAsync` | module can't see sessions; delivery is the stream's job |
| module → decision | `WaveRule` pure function | logic is testable without a grain |

When you add your own feature, reproduce these three seams. If a layer reaches *across*
a seam — a handler touching the DB, a module looping sessions, logic that can't be
tested without a grain — that is the smell this pattern exists to prevent.

---

## Checklist for your own vertical slice

- [ ] Handler validates `ctx` and does nothing else but resolve + delegate.
- [ ] The grain method you call is declared on the grain **interface**.
- [ ] State mutation / DB access is inside the grain, never the handler.
- [ ] Broadcasts go through `SendComposerToRoomAsync`; you never enumerate sessions.
- [ ] The real *decision* is a pure function you can test without Orleans.
- [ ] The first test you write is the **failure** path.
