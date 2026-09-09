using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Vortex.Primitives;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Rooms.Object.Furniture.Floor;
using Vortex.Rooms.Tests.Support;
using Xunit;

namespace Vortex.Rooms.Tests.Mapping;

/// <summary>
/// What a room recomputes when an item changes without moving.
/// </summary>
/// <remarks>
/// <para>
/// A tile's height and its walk/sit/lay flags are derived from the furniture standing on it. Change
/// an item's height or its solidity and every tile of its footprint is out of date — but both paths
/// that do it recomputed a single tile, the one the item's coordinates name. For a 1x1 that is the
/// whole footprint, which is why it survived; for the 2x2, 4x4 and larger families the rest of the
/// footprint kept the height and flags it had before the change.
/// </para>
/// <para>
/// The reach is what makes it worth a primitive rather than a fix per caller:
/// <c>FurnitureFloorLogic.OnStateChangedAsync</c> is the base of every floor logic in the hotel and
/// it goes through <c>RefreshTile</c>, so this was one tile per state change on every multi-tile
/// furni there is.
/// </para>
/// </remarks>
public sealed class FloorItemFootprintRecomputeTests
{
    private const int AnchorX = 5;
    private const int AnchorY = 6;

    private static readonly RoomObjectId Placed = new(63);

    /// <summary>A height the item cannot already have, so a tile still answering the old one is
    /// unambiguous.</summary>
    private static readonly Altitude Raised = Altitude.FromValue(3.0);

    [Fact]
    public async Task ChangingOnlyTheHeight_RecomputesEveryTileOfTheFootprint()
    {
        (RoomHarness harness, IRoomFloorItem item) = await RoomWith2x2Async().ConfigureAwait(true);

        harness.Grain.MapModule.MoveFloorItem(
            item,
            harness.Grain.MapModule.ToIdx(AnchorX, AnchorY),
            Raised
        );

        HeightsUnder(harness, item).Should().AllBeEquivalentTo(Raised);
    }

    /// <summary>
    /// The same through the context every floor logic uses. <c>OnStateChangedAsync</c> calls this on
    /// every state change, which is what gave the defect its reach.
    /// </summary>
    [Fact]
    public async Task RefreshingTheTileFromALogic_RecomputesEveryTileOfTheFootprint()
    {
        (RoomHarness harness, IRoomFloorItem item) = await RoomWith2x2Async().ConfigureAwait(true);

        // What a state change does before it asks for the refresh.
        item.SetPositionZ(Raised);

        new RoomFloorItemContext(harness.Grain, item).RefreshTile();

        HeightsUnder(harness, item).Should().AllBeEquivalentTo(Raised);
    }

    /// <summary>The control: a 1x1 was always correct, and still is.</summary>
    [Fact]
    public async Task ASingleTileItem_IsUnaffected()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);

        harness.LogicFactory = TestFloorItems.WorkingLogic;

        IRoomFloorItem item = TestFloorItems.Movable(Placed, RoomHarness.Owner);

        await PlaceAsync(harness, item).ConfigureAwait(true);

        harness.Grain.MapModule.MoveFloorItem(
            item,
            harness.Grain.MapModule.ToIdx(AnchorX, AnchorY),
            Raised
        );

        HeightsUnder(harness, item).Should().AllBeEquivalentTo(Raised);
    }

    /// <summary>The tile heights under every tile the item's id sits on — read from the stacks
    /// rather than recomputed, so this asks the room what it believes rather than what it should.
    /// </summary>
    private static List<Altitude> HeightsUnder(RoomHarness harness, IRoomFloorItem item) =>
        [
            .. Enumerable
                .Range(0, harness.Grain._state.TileFloorStacks.Length)
                .Where(idx => harness.Grain._state.TileFloorStacks[idx].Contains(item.ObjectId))
                .Select(idx => harness.Grain._state.TileHeights[idx]),
        ];

    private static async Task<(RoomHarness Harness, IRoomFloorItem Item)> RoomWith2x2Async()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);

        harness.LogicFactory = TestFloorItems.WorkingLogic;

        IRoomFloorItem item = TestFloorItems.Movable(Placed, RoomHarness.Owner, 2, 2);

        await PlaceAsync(harness, item).ConfigureAwait(true);

        return (harness, item);
    }

    private static Task<bool> PlaceAsync(RoomHarness harness, IRoomFloorItem item) =>
        harness.Grain.FurniModule.PlaceFloorItemAsync(
            harness.ContextFor(RoomHarness.Owner),
            item,
            AnchorX,
            AnchorY,
            Rotation.North,
            CancellationToken.None
        );
}
