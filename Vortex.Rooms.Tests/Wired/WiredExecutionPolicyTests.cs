using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Observability;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Actions;
using Vortex.Rooms.Wired;
using Vortex.Rooms.Wired.Engine;
using Vortex.Tests.Support;
using Xunit;

namespace Vortex.Rooms.Tests.Wired;

/// <summary>
/// How often a pile may fire, and which of its effects run when it does.
/// </summary>
/// <remarks>
/// Three rules with real subtlety, none of which had a test of its own: a rolling allowance window,
/// a cycle that has to start over rather than fall silent, and a random draw that avoids what the
/// pile just ran. The last one is only testable because the policy takes its <c>Random</c> from the
/// caller — <c>Random.Shared</c> made the behaviour unpinnable.
/// </remarks>
public sealed class WiredExecutionPolicyTests
{
    private const int STACK = 7;

    // --- the allowance window ------------------------------------------------------------------

    [Fact]
    public void WithNoLimitConfigured_EveryFiringIsAllowed()
    {
        WiredExecutionPolicy policy = Build();
        WiredPolicy unlimited = new() { ExecutionLimit = 0, ExecutionWindowMs = 0 };

        for (int i = 0; i < 100; i++)
        {
            policy.TryConsumeAllowance(STACK, unlimited, i).Should().BeTrue();
        }
    }

    [Fact]
    public void ALimitedPile_FiresItsAllowanceAndThenStops()
    {
        FakeWiredRoomHost room = new();
        WiredExecutionPolicy policy = Build(room);
        WiredPolicy limited = new() { ExecutionLimit = 3, ExecutionWindowMs = 5_000 };

        policy.TryConsumeAllowance(STACK, limited, 0).Should().BeTrue();
        policy.TryConsumeAllowance(STACK, limited, 100).Should().BeTrue();
        policy.TryConsumeAllowance(STACK, limited, 200).Should().BeTrue();
        policy.TryConsumeAllowance(STACK, limited, 300).Should().BeFalse();

        room.StopReasons.Should()
            .Equal(
                [WiredStopReason.EXECUTION_LIMIT],
                "a refused firing is counted, not only silently dropped"
            );
    }

    /// <summary>
    /// The window rolls rather than resetting in buckets: a pile limited to 3 per 5 seconds can fire
    /// again 5 seconds after its third, not when some fixed bucket happens to expire. Habbo's own
    /// semantics here are UNKNOWN (OQ-6) — this pins Vortex's documented choice so it stays a choice.
    /// </summary>
    [Fact]
    public void TheWindowRolls_SoTheOldestFiringIsForgottenAsTimePasses()
    {
        WiredExecutionPolicy policy = Build();
        WiredPolicy limited = new() { ExecutionLimit = 2, ExecutionWindowMs = 1_000 };

        policy.TryConsumeAllowance(STACK, limited, 0).Should().BeTrue();
        policy.TryConsumeAllowance(STACK, limited, 500).Should().BeTrue();
        policy.TryConsumeAllowance(STACK, limited, 900).Should().BeFalse();

        // 1001 is past the first firing's window, so that slot comes back — without the second
        // firing's slot coming back with it.
        policy.TryConsumeAllowance(STACK, limited, 1_001).Should().BeTrue();
        policy.TryConsumeAllowance(STACK, limited, 1_100).Should().BeFalse();
    }

    [Fact]
    public void EachPileHasItsOwnAllowance()
    {
        WiredExecutionPolicy policy = Build();
        WiredPolicy limited = new() { ExecutionLimit = 1, ExecutionWindowMs = 5_000 };

        policy.TryConsumeAllowance(STACK, limited, 0).Should().BeTrue();
        policy.TryConsumeAllowance(STACK, limited, 1).Should().BeFalse();
        policy.TryConsumeAllowance(STACK + 1, limited, 1).Should().BeTrue();
    }

    // --- which effects run ---------------------------------------------------------------------

    [Fact]
    public void TheDefaultModeRunsEveryEffect()
    {
        WiredExecutionPolicy policy = Build();
        List<IWiredAction> actions = Actions(1, 2, 3);

        policy.ChooseActions(STACK, actions, new WiredPolicy()).Should().HaveCount(3);
    }

    [Fact]
    public void FirstOnly_RunsTheFirstEffectOfThePile()
    {
        WiredExecutionPolicy policy = Build();
        List<IWiredAction> actions = Actions(10, 20, 30);

        List<IWiredAction> chosen = policy.ChooseActions(
            STACK,
            actions,
            new WiredPolicy { EffectMode = WiredEffectModeType.FirstOnly }
        );

        chosen.Should().ContainSingle().Which.Should().BeSameAs(actions[0]);
        IdOf(chosen.Single()).Should().Be(10);
    }

    /// <summary>
    /// Unseen walks the pile once and then starts over. Falling silent after the last effect would
    /// be a pile that works for a while and then quietly stops, which is the worst of both.
    /// </summary>
    [Fact]
    public void Unseen_WalksThePileAndThenStartsAgain()
    {
        WiredExecutionPolicy policy = Build();
        List<IWiredAction> actions = Actions(10, 20, 30);
        WiredPolicy unseen = new() { EffectMode = WiredEffectModeType.Unseen };

        List<int> order =
        [
            .. Enumerable
                .Range(0, 6)
                .Select(_ => IdOf(policy.ChooseActions(STACK, actions, unseen).Single())),
        ];

        order.Take(3).Should().BeEquivalentTo([10, 20, 30], "each effect once per cycle");
        order.Skip(3).Should().BeEquivalentTo([10, 20, 30], "and then the cycle starts over");
    }

    [Fact]
    public void Random_PicksAsManyEffectsAsAsked()
    {
        WiredExecutionPolicy policy = Build(seed: 1);

        policy
            .ChooseActions(
                STACK,
                Actions(1, 2, 3, 4),
                new WiredPolicy { EffectMode = WiredEffectModeType.Random, EffectPickCount = 2 }
            )
            .Should()
            .HaveCount(2);
    }

    /// <summary>
    /// The anti-repetition rule: with "avoid the last firing", two consecutive draws of one effect
    /// out of two must not be the same effect twice. This is the rule the injected RNG exists for —
    /// on Random.Shared it would pass by luck often enough to be useless.
    /// </summary>
    [Fact]
    public void Random_AvoidsWhatThePileJustRan()
    {
        WiredExecutionPolicy policy = Build(seed: 12345);
        List<IWiredAction> actions = Actions(10, 20);
        WiredPolicy avoiding = new()
        {
            EffectMode = WiredEffectModeType.Random,
            EffectPickCount = 1,
            EffectAvoidRecentExecutions = 1,
        };

        int first = IdOf(policy.ChooseActions(STACK, actions, avoiding).Single());
        int second = IdOf(policy.ChooseActions(STACK, actions, avoiding).Single());

        second.Should().NotBe(first);
    }

    /// <summary>
    /// The history is bounded by what the add-on asks to avoid. With a window of one, the effect two
    /// firings ago is fair game again — otherwise a pile of two effects would run dry.
    /// </summary>
    [Fact]
    public void Random_ForgetsBeyondTheAvoidanceWindow()
    {
        WiredExecutionPolicy policy = Build(seed: 999);
        List<IWiredAction> actions = Actions(10, 20);
        WiredPolicy avoiding = new()
        {
            EffectMode = WiredEffectModeType.Random,
            EffectPickCount = 1,
            EffectAvoidRecentExecutions = 1,
        };

        int first = IdOf(policy.ChooseActions(STACK, actions, avoiding).Single());
        _ = policy.ChooseActions(STACK, actions, avoiding);
        int third = IdOf(policy.ChooseActions(STACK, actions, avoiding).Single());

        third.Should().Be(first, "two firings back is outside a window of one");
    }

    [Fact]
    public void AnEmptyPile_RunsNothingInEveryMode()
    {
        WiredExecutionPolicy policy = Build();

        foreach (WiredEffectModeType mode in Enum.GetValues<WiredEffectModeType>())
        {
            policy
                .ChooseActions(STACK, [], new WiredPolicy { EffectMode = mode })
                .Should()
                .BeEmpty();
        }
    }

    private static WiredExecutionPolicy Build(FakeWiredRoomHost? room = null, int seed = 0) =>
        new((room ?? new FakeWiredRoomHost()).Diagnostics, new Random(seed));

    private static int IdOf(IWiredAction action) => ((FurnitureWiredLogic)action).ObjectId.Value;

    private static List<IWiredAction> Actions(params int[] objectIds) =>
        [.. objectIds.Select(id => (IWiredAction)new TestAction(id))];

    private sealed class TestAction(int objectId)
        : FurnitureWiredActionLogic(
            FakeProxy.Create<IGrainFactory>(_ => null),
            FakeProxy.Create<IStuffDataFactory>(_ => null),
            WiredTestBoxes.Context(objectId)
        )
    {
        public override int WiredCode => 0;

        protected override Task FillInternalDataAsync(CancellationToken ct) => Task.CompletedTask;
    }
}
