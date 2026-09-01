using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Vortex.Primitives.Rooms.Mapping;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic.Furniture;
using Vortex.Rooms.Object.Avatars.Player;
using Vortex.Rooms.Tests.Support;
using Vortex.Tests.Support;
using Xunit;

namespace Vortex.Rooms.Tests.Mapping;

/// <summary>
/// The same tile answering two different heights, through the real map module.
///
/// <see cref="RoomTileSectionFinderTests" /> pins the arithmetic down on plain numbers; this pins
/// down the half that arithmetic cannot see — that <c>CollectOccupants()</c> reads a real
/// <see cref="IRoomFloorItem" /> into the right slab. <c>Z</c> is where the item rests and
/// <c>Height</c> is <c>Z + stack height</c>: get those two the wrong way round and every surface in
/// the room is wrong, with nothing in the pure tests able to notice.
/// </summary>
public sealed class RoomMapSectionTests
{
    private const int Tile = 2;

    /// <summary>A floor item filling <c>[bottom, top]</c>, and nothing else the map asks about.</summary>
    private static IRoomFloorItem Platform(int objectId, double bottom, double top)
    {
        IFurnitureFloorLogic logic = FakeProxy.Create<IFurnitureFloorLogic>(call =>
            call.Method.Name switch
            {
                nameof(IFurnitureFloorLogic.CanWalk) => true,
                nameof(IFurnitureFloorLogic.CanStack) => true,
                nameof(IFurnitureFloorLogic.CanSit) => false,
                nameof(IFurnitureFloorLogic.CanLay) => false,
                nameof(IFurnitureFloorLogic.GetPostureOffset) => Altitude.Zero,
                _ => null,
            }
        );

        return FakeProxy.Create<IRoomFloorItem>(call =>
            call.Method.Name switch
            {
                $"get_{nameof(IRoomFloorItem.ObjectId)}" => new RoomObjectId(objectId),
                $"get_{nameof(IRoomFloorItem.Z)}" => Altitude.FromValue(bottom),
                $"get_{nameof(IRoomFloorItem.Height)}" => Altitude.FromValue(top),
                $"get_{nameof(IRoomFloorItem.Logic)}" => logic,
                _ => null,
            }
        );
    }

    private static async Task<(RoomHarness Harness, int TileId)> RoomWithPlatformAsync(
        double bottom,
        double top
    )
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        int tileId = harness.Grain.MapModule.ToIdx(Tile, Tile);
        IRoomFloorItem platform = Platform(1, bottom, top);

        harness.Grain._state.ItemsById[platform.ObjectId] = platform;
        harness.Grain._state.TileFloorStacks[tileId].Add(platform.ObjectId);

        // Not optional, and not decoration: this is what makes TileHeights hold the platform's top
        // rather than the model's floor. Without it GetTopSection() answers zero, the tile looks
        // bare to anything reading the flat arrays, and a test meant to prove the search no longer
        // reads the top would pass whether it did or not.
        harness.Grain.MapModule.ComputeTile(tileId);

        return (harness, tileId);
    }

    /// <summary>
    /// The feature itself. A platform resting two units up is walked *under* by somebody at floor
    /// level and walked *on* by somebody already up there — one tile, two answers, chosen by where
    /// the asker's feet are.
    /// </summary>
    [Fact]
    public async Task ARaisedPlatform_OffersTheFloorBelowAndItsOwnTop()
    {
        (RoomHarness harness, int tileId) = await RoomWithPlatformAsync(bottom: 2, top: 3)
            .ConfigureAwait(true);

        RoomTileSection? fromTheFloor = harness.Grain.MapModule.FindSection(
            tileId,
            Altitude.Zero,
            Altitude.FromValue(2)
        );

        fromTheFloor.Should().NotBeNull();
        fromTheFloor!.Value.Height.Should().Be(Altitude.Zero);
        fromTheFloor
            .Value.IsBareFloor.Should()
            .BeTrue("there is two units of air under the platform");

        RoomTileSection? fromAbove = harness.Grain.MapModule.FindSection(
            tileId,
            Altitude.FromValue(3),
            Altitude.FromValue(2)
        );

        fromAbove.Should().NotBeNull();
        fromAbove!.Value.Height.Should().Be(Altitude.FromValue(3));
        fromAbove.Value.ItemId.Value.Should().Be(1);
        fromAbove.Value.IsWalkable.Should().BeTrue();
    }

    /// <summary>
    /// One unit of headroom is not a crawlspace: the tile stops offering its floor, so the
    /// pathfinder routes around instead of walking somebody into it.
    /// </summary>
    [Fact]
    public async Task APlatformTooLow_ClosesTheTileToSomebodyOnTheFloor()
    {
        (RoomHarness harness, int tileId) = await RoomWithPlatformAsync(bottom: 1, top: 2)
            .ConfigureAwait(true);

        harness
            .Grain.MapModule.FindSection(tileId, Altitude.Zero, Altitude.FromValue(0))
            .Should()
            .BeNull();
    }

    /// <summary>
    /// Under a platform and still able to leave.
    ///
    /// The search starts from the avatar's own altitude, not the tile's highest surface. Reading
    /// the top told it that somebody standing *under* a platform was standing *on* it, so every
    /// neighbouring floor tile was three units below the foot it thought it had, nothing was within
    /// a step, and the walk came back empty — you could get under a piece of furniture and then not
    /// get out again.
    /// </summary>
    [Fact]
    public async Task StandingUnderAPlatform_CanStillWalkOutFromUnderIt()
    {
        (RoomHarness harness, int tileId) = await RoomWithPlatformAsync(bottom: 2, top: 3)
            .ConfigureAwait(true);

        RoomPlayerAvatar avatar = harness.PutRealPlayerInRoom(1, Tile, Tile);
        avatar.SetHeight(Altitude.Zero);

        harness.Grain.MapModule.AddAvatar(avatar, false);

        IReadOnlyList<(int X, int Y)> out_ = harness.Grain.PathingSystem.FindPath(
            avatar,
            (Tile, Tile),
            (Tile + 2, Tile)
        );

        out_.Should().NotBeEmpty("the floor around the platform is at the same height as under it");
        out_[^1].X.Should().Be(Tile + 2);
        out_[^1].Y.Should().Be(Tile);

        // And the tile is genuinely the one with the platform on it, so the walk really did start
        // underneath rather than from somewhere the platform does not cover.
        harness.Grain._state.TileFloorStacks[tileId].Should().NotBeEmpty();
    }

    /// <summary>
    /// Clicking the platform above your head, while standing under it.
    ///
    /// The click carries no height — the floor under a platform and the top of that platform are
    /// the same (x, y) — so the request arrives as the tile the avatar is already standing on, and
    /// was refused as "you are already there". It is a real request: the other surface. No step
    /// joins two surfaces of one tile directly, so the answer is a route that leaves by a
    /// neighbour and comes back at the other height.
    /// </summary>
    [Fact]
    public async Task ClickingTheTileYouAreOn_MeansItsOtherSurface()
    {
        (RoomHarness harness, int tileId) = await RoomWithPlatformAsync(bottom: 1, top: 1)
            .ConfigureAwait(true);

        RoomPlayerAvatar avatar = harness.PutRealPlayerInRoom(1, Tile, Tile);
        avatar.SetHeight(Altitude.Zero);

        harness.Grain.MapModule.AddAvatar(avatar, false);

        IReadOnlyList<(int X, int Y)> path = harness.Grain.PathingSystem.FindPath(
            avatar,
            (Tile, Tile),
            (Tile, Tile)
        );

        path.Should().NotBeEmpty("the tile has a second surface, so this is not standing still");
        path[^1].X.Should().Be(Tile);
        path[^1].Y.Should().Be(Tile);
        path.Count.Should().BeGreaterThan(2, "leaving and returning cannot be done in one step");

        _ = tileId;
    }

    /// <summary>
    /// Clicking a raised item takes you onto it, not into the gap beneath it.
    ///
    /// The crawlspace is the cheaper arrival of the two, so "first arrival wins" chose it. A click
    /// means the thing clicked, so the goal is the highest surface that can be reached.
    /// </summary>
    [Fact]
    public async Task WalkingToARaisedItem_ArrivesOnTopOfItRatherThanUnderneath()
    {
        (RoomHarness harness, int tileId) = await RoomWithPlatformAsync(bottom: 1, top: 1)
            .ConfigureAwait(true);

        RoomPlayerAvatar avatar = harness.PutRealPlayerInRoom(1, 0, 0);
        avatar.SetHeight(Altitude.Zero);

        harness.Grain.MapModule.AddAvatar(avatar, false);

        IReadOnlyList<(int X, int Y)> path = harness.Grain.PathingSystem.FindPath(
            avatar,
            (0, 0),
            (Tile, Tile)
        );

        path.Should().NotBeEmpty();
        path[^1].X.Should().Be(Tile);
        path[^1].Y.Should().Be(Tile);

        RoomTileSection? arrival = harness.Grain.MapModule.FindSection(
            tileId,
            Altitude.Zero,
            Altitude.FromValue(2)
        );

        arrival.Should().NotBeNull();
        arrival!.Value.Height.Should().Be(Altitude.FromValue(1), "the item's top, not the floor");
        arrival.Value.ItemId.Value.Should().Be(1);
    }

    /// <summary>
    /// A tile the walk cannot reach at any of its heights is not a step, which is the answer the
    /// pathfinder needs in order to go round a cliff rather than into it.
    /// </summary>
    [Fact]
    public async Task ASurfaceOutOfStep_IsNotOffered()
    {
        (RoomHarness harness, int tileId) = await RoomWithPlatformAsync(bottom: 8, top: 9)
            .ConfigureAwait(true);

        RoomTileSection? section = harness.Grain.MapModule.FindSection(
            tileId,
            Altitude.FromValue(9),
            Altitude.FromValue(2)
        );

        section.Should().NotBeNull("standing on the platform, its own top is under the foot");
        section!.Value.Height.Should().Be(Altitude.FromValue(9));

        harness
            .Grain.MapModule.FindSection(tileId, Altitude.FromValue(4), Altitude.FromValue(2))
            .Should()
            .BeNull("neither the floor nor the platform is within two units of 4");
    }

    /// <summary>
    /// The height on the wire is what tells "walk up onto the thing above me" apart from "you are
    /// already standing here".
    ///
    /// Both arrive as the tile the avatar is on, so the server used to answer them the same way:
    /// any tile with a second surface started a walk, including a click on the surface already
    /// under the player's feet. With MoveAvatarMessageComposer carrying the clicked altitude, the
    /// no-op case is exactly "the surface asked for is the one stood on", and nothing else.
    /// </summary>
    [Fact]
    public async Task ClickingTheSurfaceAlreadyStoodOn_IsRefusedOnceTheHeightIsKnown()
    {
        // The same fixture ClickingTheTileYouAreOn_MeansItsOtherSurface uses, and for the same
        // reason: its second surface is genuinely reachable. With one that is not, the walk fails
        // later for want of a route and returns false whatever the guard decided -- the test would
        // pass with the height field ripped out, which is no test at all.
        (RoomHarness harness, int tileId) = await RoomWithPlatformAsync(bottom: 1, top: 1)
            .ConfigureAwait(true);

        RoomPlayerAvatar avatar = harness.PutRealPlayerInRoom(1, Tile, Tile);
        avatar.SetHeight(Altitude.Zero);

        harness.Grain.MapModule.AddAvatar(avatar, false);

        int floorKey = 0;
        int topKey = (int)Math.Round(harness.Grain.MapModule.GetTopSection(tileId).Height * 100);

        topKey.Should().NotBe(floorKey, "the fixture is a tile with two surfaces");

        bool ontoTheFloorItIsOn = await harness
            .Grain.AvatarModule.WalkAvatarToAsync(avatar, Tile, Tile, CancellationToken.None, floorKey)
            .ConfigureAwait(true);

        ontoTheFloorItIsOn
            .Should()
            .BeFalse("the surface clicked is the one already stood on, so there is nothing to walk");

        // The other surface of the same tile still starts a walk: this is a real request, and it
        // is what makes the assertion above mean something -- without the height both calls take
        // this branch, so the refusal is the height's doing and nothing else's.
        bool ontoThePlatformAbove = await harness
            .Grain.AvatarModule.WalkAvatarToAsync(avatar, Tile, Tile, CancellationToken.None, topKey)
            .ConfigureAwait(true);

        ontoThePlatformAbove
            .Should()
            .BeTrue("the tile's other surface is somewhere else to stand, so the walk goes ahead");
    }
}
