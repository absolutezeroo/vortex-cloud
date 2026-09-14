using System.Collections.Generic;
using FluentAssertions;
using Vortex.Primitives.Action;
using Vortex.Primitives.Rooms;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Events;
using Vortex.Primitives.Rooms.Events.Player;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Rooms.Wired;
using Vortex.Tests.Support;
using Xunit;

namespace Vortex.Rooms.Tests.Wired;

/// <summary>
/// What the event that started a chain tells the <c>@event.…</c> variables.
/// </summary>
/// <remarks>
/// The arithmetic is one subtraction, but the two properties around it are worth holding: the
/// difference has to keep its sign, because "went down by three" is a chain a builder writes; and a
/// chain started by some other kind of event must leave the entries ABSENT rather than zero, or a
/// variable that really did change to zero becomes indistinguishable from one that never changed at
/// all.
/// </remarks>
public sealed class WiredEventReadingsTests
{
    [Theory]
    [InlineData(10, 14, 4)]
    [InlineData(14, 10, -4)]
    [InlineData(7, 7, 0)]
    public void AVariableChangeIsReadableAsBeforeAfterAndDifference(
        int previous,
        int current,
        int difference
    )
    {
        IWiredContext context = EmptyContext();

        WiredEventReadings.Populate(context, Changed(previous, current));

        context.Variables[WiredEventReadings.VariableOldValue].Should().Be(previous);
        context.Variables[WiredEventReadings.VariableNewValue].Should().Be(current);
        context
            .Variables[WiredEventReadings.VariableDifference]
            .Should()
            .Be(difference, "a drop is a chain a builder writes, so the sign has to survive");
    }

    [Fact]
    public void AnEventOfAnotherKindLeavesTheReadingsAbsent_NotZero()
    {
        IWiredContext context = EmptyContext();

        WiredEventReadings.Populate(
            context,
            new PlayerChatEvent
            {
                RoomId = new RoomId(1),
                CausedBy = ActionContext.CreateForWired(new RoomId(1)),
                PlayerId = new Primitives.Players.PlayerId(1),
                Message = "hello",
            }
        );

        context
            .Variables.Should()
            .BeEmpty("a real change to zero must not look like a chain that was never about one");
    }

    /// <summary>A context that is nothing but its variable bag, which is all Populate touches.</summary>
    private static IWiredContext EmptyContext()
    {
        Dictionary<string, int> variables = [];

        return FakeProxy.Create<IWiredContext>(call =>
            call.Method.Name == "get_Variables" ? variables : null
        );
    }

    private static WiredVariableChangedEvent Changed(int previous, int current) =>
        new()
        {
            RoomId = new RoomId(1),
            CausedBy = ActionContext.CreateForWired(new RoomId(1)),
            Key = new WiredVariableKey(default, WiredVariableTargetType.Global, 0),
            Kind = WiredVariableChangeKind.ValueChanged,
            Previous = previous,
            Current = current,
        };
}
