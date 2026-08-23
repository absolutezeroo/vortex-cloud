using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Vortex.Primitives.Players;
using Vortex.Protocol.Messages.Outgoing.Room.Action;
using Vortex.Rooms.Grains.Modules;
using Vortex.Rooms.Object.Avatars.Player;
using Vortex.Rooms.Tests.Support;
using Xunit;

namespace Vortex.Rooms.Tests.Avatars;

/// <summary>
/// Hand items: the drink an avatar holds for a while. Nothing about one is persisted, so what
/// matters is that it reaches the room, leaves again when its time is up, and that passing it moves
/// it rather than copying it.
/// </summary>
public sealed class RoomHandItemTests
{
    private const int Water = 7;

    private static readonly PlayerId Giver = new(101);
    private static readonly PlayerId Taker = new(202);

    [Fact]
    public async Task GivingAnItem_PutsItInTheHandAndTellsTheRoom()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        RoomPlayerAvatar avatar = harness.PutRealPlayerInRoom(Giver, 2, 2);

        harness.BroadcastToRoom.Clear();

        harness.Grain.HandItemModule.Give(Giver, Water).Should().BeTrue();

        avatar.CarryItemId.Should().Be(Water);

        harness
            .BroadcastToRoom.OfType<CarryObjectMessageComposer>()
            .Should()
            .ContainSingle()
            .Which.ItemType.Should()
            .Be(Water);
    }

    [Fact]
    public async Task AHandItem_LeavesTheHandOnceItsTimeIsUp()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        RoomPlayerAvatar avatar = harness.PutRealPlayerInRoom(Giver, 2, 2);

        harness.Grain.HandItemModule.Give(Giver, Water);
        harness.BroadcastToRoom.Clear();

        await harness.TickAvatarsAsync(1).ConfigureAwait(true);

        avatar.CarryItemId.Should().Be(Water, "the drink has only just been handed over");

        await harness.TickAvatarsPastAsync(RoomHarness.HandItemDurationMs).ConfigureAwait(true);

        avatar.CarryItemId.Should().Be(0);

        harness
            .BroadcastToRoom.OfType<CarryObjectMessageComposer>()
            .Should()
            .Contain(
                composer => composer.ItemType == 0,
                "an emptied hand is not in the avatar block, so it has to be said out loud"
            );
    }

    [Fact]
    public async Task DroppingAnItem_EmptiesTheHand()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        RoomPlayerAvatar avatar = harness.PutRealPlayerInRoom(Giver, 2, 2);

        harness.Grain.HandItemModule.Give(Giver, Water);
        harness.BroadcastToRoom.Clear();

        harness.Grain.HandItemModule.Drop(Giver).Should().BeTrue();

        avatar.CarryItemId.Should().Be(0);
        harness
            .BroadcastToRoom.OfType<CarryObjectMessageComposer>()
            .Should()
            .ContainSingle()
            .Which.ItemType.Should()
            .Be(0);
    }

    [Fact]
    public async Task DroppingNothing_IsRefusedRatherThanBroadcast()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        harness.PutRealPlayerInRoom(Giver, 2, 2);

        harness.BroadcastToRoom.Clear();

        harness.Grain.HandItemModule.Drop(Giver).Should().BeFalse();
        harness.BroadcastToRoom.Should().BeEmpty();
    }

    [Fact]
    public async Task PassingAnItem_MovesItRatherThanCopyingIt()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        RoomPlayerAvatar giver = harness.PutRealPlayerInRoom(Giver, 2, 2);
        RoomPlayerAvatar taker = harness.PutRealPlayerInRoom(Taker, 2, 3);

        harness.Grain.HandItemModule.Give(Giver, Water);

        harness.Grain.HandItemModule.Pass(Giver, Taker).Should().BeTrue();

        giver.CarryItemId.Should().Be(0, "two people holding the same drink is the bug to avoid");
        taker.CarryItemId.Should().Be(Water);
    }

    [Fact]
    public async Task PassingAcrossTheRoom_IsRefused()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        RoomPlayerAvatar giver = harness.PutRealPlayerInRoom(Giver, 2, 2);
        RoomPlayerAvatar taker = harness.PutRealPlayerInRoom(Taker, 9, 9);

        harness.Grain.HandItemModule.Give(Giver, Water);

        harness
            .Grain.HandItemModule.Pass(Giver, Taker)
            .Should()
            .BeFalse("the client only offers the button on somebody within reach");

        giver.CarryItemId.Should().Be(Water);
        taker.CarryItemId.Should().Be(0);
    }

    [Fact]
    public async Task PassingAnEmptyHand_IsRefused()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        harness.PutRealPlayerInRoom(Giver, 2, 2);
        RoomPlayerAvatar taker = harness.PutRealPlayerInRoom(Taker, 2, 3);

        harness.Grain.HandItemModule.Pass(Giver, Taker).Should().BeFalse();
        taker.CarryItemId.Should().Be(0);
    }

    [Fact]
    public async Task PassingToSomebodyWhoIsNotHere_IsRefused()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        RoomPlayerAvatar giver = harness.PutRealPlayerInRoom(Giver, 2, 2);

        harness.Grain.HandItemModule.Give(Giver, Water);

        harness.Grain.HandItemModule.Pass(Giver, Taker).Should().BeFalse();
        giver.CarryItemId.Should().Be(Water, "a pass that went nowhere must not empty the hand");
    }

    [Fact]
    public async Task ThePlayersOwnSnapshot_CarriesWhatTheyHold()
    {
        // Room entry replays hand items off the snapshot, the same way it replays dances.
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        RoomPlayerAvatar avatar = harness.PutRealPlayerInRoom(Giver, 2, 2);

        harness.Grain.HandItemModule.Give(Giver, Water);

        avatar
            .GetSnapshot()
            .Should()
            .BeOfType<Primitives.Rooms.Snapshots.Avatars.RoomPlayerAvatarSnapshot>();
        ((Primitives.Rooms.Snapshots.Avatars.RoomPlayerAvatarSnapshot)avatar.GetSnapshot())
            .CarryItemId.Should()
            .Be(Water);
    }

    [Theory]
    [InlineData(0, 0, 0, 0, true)]
    [InlineData(0, 0, 1, 1, true)]
    [InlineData(0, 0, 2, 0, false)]
    [InlineData(5, 5, 5, 7, false)]
    public void ReachIsOneTileInAnyDirection(
        int fromX,
        int fromY,
        int toX,
        int toY,
        bool expected
    ) => RoomHandItemModule.IsWithinReach(fromX, fromY, toX, toY).Should().Be(expected);
}
