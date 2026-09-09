using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Rooms.Tests.Support;
using Xunit;

namespace Vortex.Rooms.Tests.Furniture;

/// <summary>
/// What the room is left holding when an attach does not finish.
/// </summary>
/// <remarks>
/// <para>
/// <c>AttatchObjectAsync</c> puts the item in <c>ItemsById</c> on its first line, and everything
/// after that can fail: the owner name is a grain call, the logic comes from a provider that can
/// refuse a definition's logic name, <c>OnAttachAsync</c> is behaviour code, and the map add answers
/// a bool. All of it used to return false or throw with the item still published -- with no logic,
/// no footprint, and no add composer ever broadcast, so no client had ever heard of it.
/// </para>
/// <para>
/// That item was then unreachable and immovable at once: every use site dereferenced its null logic,
/// and placing it again hit the <c>TryAdd</c> that refuses a duplicate, which throws
/// <c>FloorItemNotFound</c>. One transient failure and the object was unplaceable until the room
/// unloaded.
/// </para>
/// </remarks>
public sealed class FloorItemAttachAtomicityTests
{
    private static readonly RoomObjectId Placed = new(41);

    [Fact]
    public async Task AnAttachWhoseLogicThrows_LeavesTheRoomAsItFoundIt()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);

        harness.LogicFactory = TestFloorItems.LogicThatFailsToAttach;

        Func<Task> place = () => PlaceAsync(harness, Item());

        await place.Should().ThrowAsync<InvalidOperationException>().ConfigureAwait(true);

        harness.Grain._state.ItemsById.Should().NotContainKey(Placed);
        Violations(harness).Should().BeEmpty();
    }

    /// <summary>
    /// The other exit. An item that already carries a logic is refused by the attach rather than
    /// throwing, and that path had the same problem: it answered false with the item published.
    /// </summary>
    [Fact]
    public async Task AnAttachThatIsRefused_LeavesTheRoomAsItFoundIt()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);

        harness.LogicFactory = TestFloorItems.WorkingLogic;

        bool placed = await PlaceAsync(
                harness,
                TestFloorItems.Movable(
                    Placed,
                    RoomHarness.Owner,
                    preAttachedLogic: TestFloorItems.WorkingLogic()
                )
            )
            .ConfigureAwait(true);

        placed.Should().BeFalse();

        harness.Grain._state.ItemsById.Should().NotContainKey(Placed);
        Violations(harness).Should().BeEmpty();
    }

    /// <summary>
    /// The consequence that made this worth fixing rather than logging: the ghost held the id, so
    /// the retry every player makes -- drag it out of the hand again -- hit TryAdd and threw.
    /// </summary>
    [Fact]
    public async Task AfterAFailedAttach_TheSameItemCanStillBePlaced()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);

        harness.LogicFactory = TestFloorItems.LogicThatFailsToAttach;

        Func<Task> failing = () => PlaceAsync(harness, Item());

        await failing.Should().ThrowAsync<InvalidOperationException>().ConfigureAwait(true);

        // The hotel recovers -- whatever refused the logic is over -- and the player drags the same
        // item back in.
        harness.LogicFactory = TestFloorItems.WorkingLogic;

        bool placed = await PlaceAsync(harness, Item()).ConfigureAwait(true);

        placed.Should().BeTrue();
        harness.Grain._state.ItemsById.Should().ContainKey(Placed);
        Violations(harness).Should().BeEmpty();
    }

    /// <summary>A multi-tile item fails the same way, and leaves no part of its footprint behind.</summary>
    [Fact]
    public async Task AFailedMultiTileAttach_LeavesNoFootprint()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);

        harness.LogicFactory = TestFloorItems.LogicThatFailsToAttach;

        Func<Task> place = () =>
            PlaceAsync(harness, TestFloorItems.Movable(Placed, RoomHarness.Owner, 2, 2));

        await place.Should().ThrowAsync<InvalidOperationException>().ConfigureAwait(true);

        Violations(harness).Should().BeEmpty();
    }

    /// <summary>And the healthy path still leaves the room consistent, so the tests above are not
    /// passing because nothing ever gets attached.</summary>
    [Fact]
    public async Task ASuccessfulAttach_LeavesTheRoomConsistent()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);

        harness.LogicFactory = TestFloorItems.WorkingLogic;

        bool placed = await PlaceAsync(
                harness,
                TestFloorItems.Movable(Placed, RoomHarness.Owner, 2, 2)
            )
            .ConfigureAwait(true);

        placed.Should().BeTrue();
        Violations(harness).Should().BeEmpty();
    }

    private static Task<bool> PlaceAsync(RoomHarness harness, IRoomFloorItem item) =>
        harness.Grain.FurniModule.PlaceFloorItemAsync(
            harness.ContextFor(RoomHarness.Owner),
            item,
            5,
            6,
            Rotation.North,
            CancellationToken.None
        );

    private static IRoomFloorItem Item() => TestFloorItems.Movable(Placed, RoomHarness.Owner);

    private static IReadOnlyList<string> Violations(RoomHarness harness) =>
        RoomFurnitureInvariants.Violations(harness.Grain._state);
}
