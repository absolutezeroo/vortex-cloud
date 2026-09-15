using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Orleans;
using Vortex.Furniture;
using Vortex.Furniture.Providers;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Grains;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Snapshots.Wired.Variables;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Variables;
using Vortex.Rooms.Wired;
using Vortex.Rooms.Wired.Variables;
using Vortex.Tests.Support;
using Xunit;

namespace Vortex.Rooms.Tests.Wired;

/// <summary>
/// The reference-variable dropdown reads other rooms' shared variables out of the database, without
/// activating those rooms, so it rebuilds each variable from the furni row instead of asking the box
/// that owns it. That is the same declaration written in two places, and these are the tests that
/// keep them the same one: each hydrates the real box and compares its own snapshot against the
/// rebuilt one, field for field.
/// </summary>
public sealed class WiredSharedVariableIndexTests
{
    private const int BoxId = 4242;

    [Fact]
    public async Task RoomVariable_RebuiltFromTheRow_MatchesTheBoxsOwnDeclaration()
    {
        WiredData data = new()
        {
            IntParams = [(int)WiredAvailabilityType.Shared],
            StringParam = "score",
        };

        WiredVariableRoom box = new(SharedGrains(), new StuffDataFactory(), Context(data));

        await box.LoadWiredAsync(CancellationToken.None);

        WiredSharedVariableIndex
            .TryDescribeShared(LogicOf(box), BoxId, data, out WiredVariableSnapshot rebuilt)
            .Should()
            .BeTrue();

        rebuilt.Should().BeEquivalentTo(box.GetVarSnapshot());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public async Task UserVariable_RebuiltFromTheRow_MatchesTheBoxsOwnDeclaration(int hasValue)
    {
        // Int param 1 is the box's "holds a value" checkbox, and it decides two of the flags the
        // client reads off the wire — so the rebuild has to follow it, not assume it.
        WiredData data = new()
        {
            IntParams = [(int)WiredAvailabilityType.Shared, hasValue],
            StringParam = "level",
        };

        WiredVariableUser box = new(SharedGrains(), new StuffDataFactory(), Context(data));

        await box.LoadWiredAsync(CancellationToken.None);

        WiredSharedVariableIndex
            .TryDescribeShared(LogicOf(box), BoxId, data, out WiredVariableSnapshot rebuilt)
            .Should()
            .BeTrue();

        rebuilt.Should().BeEquivalentTo(box.GetVarSnapshot());
    }

    [Fact]
    public void AVariableThatIsNotShared_IsNotOffered()
    {
        WiredData data = new()
        {
            IntParams = [(int)WiredAvailabilityType.Persistent],
            StringParam = "score",
        };

        WiredSharedVariableIndex
            .TryDescribeShared("wf_var_room", BoxId, data, out _)
            .Should()
            .BeFalse();
    }

    [Fact]
    public void ABoxThatCannotShare_IsNotOffered()
    {
        // A furni whose logic is not one of the two sharing boxes never contributes, whatever its
        // int params happen to say — the query filters on the logic key, and so does this.
        WiredData data = new()
        {
            IntParams = [(int)WiredAvailabilityType.Shared],
            StringParam = "score",
        };

        WiredSharedVariableIndex
            .TryDescribeShared("wf_var_reference", BoxId, data, out _)
            .Should()
            .BeFalse();

        WiredSharedVariableIndex.SharedCapableLogics.Should().NotContain("wf_var_reference");
    }

    [Fact]
    public void AnUnconfiguredBox_IsNotOffered()
    {
        WiredSharedVariableIndex
            .TryDescribeShared("wf_var_room", BoxId, new WiredData(), out _)
            .Should()
            .BeFalse();
    }

    /// <summary>
    /// A grain factory that answers, because a shared box now needs one.
    /// </summary>
    /// <remarks>
    /// Shared is the availability whose values live behind a grain rather than in this room, so the
    /// box reaches for one while it hydrates. These tests are about what the box <em>declares</em>,
    /// not about what it holds, so the grain is never asked anything — but it has to exist.
    /// </remarks>
    private static IGrainFactory SharedGrains() =>
        FakeProxy.Create<IGrainFactory>(call =>
            call.Method.Name == nameof(IGrainFactory.GetGrain)
                ? FakeProxy.Create<IWiredSharedVariableGrain>(_ => null)
                : null
        );

    /// <summary>The box as the database holds it: its wired configuration, as the furni's extra
    /// data, which is exactly what the query below reads back.</summary>
    private static IRoomFloorItemContext Context(WiredData data) =>
        WiredTestBoxes.Context(
            objectId: BoxId,
            extraData: new ExtraData(
                JsonSerializer.Serialize(
                    new Dictionary<string, WiredData> { [ExtraDataSectionType.WIRED] = data }
                )
            )
        );

    private static string LogicOf(object box) =>
        box switch
        {
            WiredVariableRoom => "wf_var_room",
            WiredVariableUser => "wf_var_user",
            _ => string.Empty,
        };
}
