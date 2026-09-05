using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Vortex.Furniture;
using Vortex.Furniture.Providers;
using Vortex.Primitives.Furniture;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.Snapshots;
using Vortex.Primitives.Furniture.StuffData;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Orleans.Snapshots.Players;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Protocol.Messages.Outgoing.Room.Engine;
using Vortex.Rooms.Object.Avatars.Player;
using Vortex.Rooms.Object.Logic.Furniture.Floor;
using Vortex.Tests.Support;
using Xunit;

namespace Vortex.Rooms.Tests.Furniture;

/// <summary>
/// The clothing-change booth (<c>fball_gate</c>): step on to put the kit on, step on again to take
/// it off.
/// </summary>
/// <remarks>
/// Two failures worth pinning. The booth used to bind to nothing at all, so walking onto it did
/// nothing and no test could tell — these drive the real logic through its real walk-on. And the
/// worn look must never outlive the visit: it lives on the room avatar precisely so that leaving
/// undresses you, which is the difference between a booth and a way to overwrite somebody's saved
/// figure.
/// </remarks>
public sealed class ClothingChangeBoothTests
{
    private const string OwnLook = "hd-180-1.hr-828-45.ch-999-1.lg-999-1";
    private const string BoyKit = "ch-210-66.lg-270-82";
    private const string GirlKit = "ch-665-92.lg-716-82";

    [Fact]
    public async Task StepIn_PutsTheKitOn_AndTellsTheRoom()
    {
        List<IComposer> sent = [];
        FurnitureClothingChangeLogic booth = Booth($"{BoyKit},{GirlKit}", sent);
        RoomPlayerAvatar player = Player(AvatarGenderType.Male);

        await booth.OnWalkOnAsync(WalkerOn(player), CancellationToken.None);

        player.Figure.Should().Be("hd-180-1.hr-828-45.ch-210-66.lg-270-82");
        sent.Should()
            .ContainSingle()
            .Which.Should()
            .BeOfType<UserChangeMessageComposer>()
            .Which.Figure.Should()
            .Be(
                player.Figure,
                "everyone in the room has to see the kit go on, not just the wearer"
            );
    }

    [Fact]
    public async Task AGirl_GetsTheGirlsSide()
    {
        FurnitureClothingChangeLogic booth = Booth($"{BoyKit},{GirlKit}", []);
        RoomPlayerAvatar player = Player(AvatarGenderType.Female);

        await booth.OnWalkOnAsync(WalkerOn(player), CancellationToken.None);

        player.Figure.Should().Be("hd-180-1.hr-828-45.ch-665-92.lg-716-82");
    }

    [Fact]
    public async Task StepInAgain_TakesItOff()
    {
        FurnitureClothingChangeLogic booth = Booth($"{BoyKit},{GirlKit}", []);
        RoomPlayerAvatar player = Player(AvatarGenderType.Male);

        await booth.OnWalkOnAsync(WalkerOn(player), CancellationToken.None);
        await booth.OnWalkOnAsync(WalkerOn(player), CancellationToken.None);

        // Exactly the look they arrived in, not a re-merge of it.
        player.Figure.Should().Be(OwnLook);
    }

    [Fact]
    public async Task AnUnconfiguredSide_DressesNobody()
    {
        // A booth whose girls' half was never set. Dressing her in an empty outfit would leave a
        // floating head, and there would be no way back out of it.
        List<IComposer> sent = [];
        FurnitureClothingChangeLogic booth = Booth($"{BoyKit},", sent);
        RoomPlayerAvatar player = Player(AvatarGenderType.Female);

        await booth.OnWalkOnAsync(WalkerOn(player), CancellationToken.None);

        player.Figure.Should().Be(OwnLook);
        sent.Should().BeEmpty("nothing changed, so the room is told nothing");
    }

    [Fact]
    public async Task SavingANewLookWhileDressed_EndsTheOverride()
    {
        FurnitureClothingChangeLogic booth = Booth($"{BoyKit},{GirlKit}", []);
        RoomPlayerAvatar player = Player(AvatarGenderType.Male);

        await booth.OnWalkOnAsync(WalkerOn(player), CancellationToken.None);

        // The player edits their look in the client while standing in the kit.
        player.UpdateWithPlayer(Snapshot(AvatarGenderType.Male, "hd-185-2.ch-111-1"));

        await booth.OnWalkOnAsync(WalkerOn(player), CancellationToken.None);

        // The next step DRESSES rather than restoring: the remembered look no longer exists
        // anywhere, and putting them back into it would undo the edit they just made.
        player.Figure.Should().Be("hd-185-2.ch-210-66.lg-270-82");
    }

    // ---- helpers ------------------------------------------------------------

    private static RoomPlayerAvatar Player(AvatarGenderType gender)
    {
        RoomPlayerAvatar avatar = new() { ObjectId = new RoomObjectId(1), PlayerId = new(1) };
        avatar.UpdateWithPlayer(Snapshot(gender, OwnLook));

        return avatar;
    }

    private static PlayerSummarySnapshot Snapshot(AvatarGenderType gender, string figure) =>
        new()
        {
            PlayerId = new(1),
            Name = "Tester",
            Motto = string.Empty,
            Figure = figure,
            Gender = gender,
            AchievementScore = 0,
            CreatedAt = DateTime.UnixEpoch,
        };

    /// <summary>The context the room really hands a walk-on: a PLAYER context, which is the only
    /// kind <c>ActionContext.CreateForObjectContext</c> accepts and therefore the only kind the base
    /// walk-on survives.</summary>
    private static IRoomPlayerContext WalkerOn(RoomPlayerAvatar player) =>
        FakeProxy.Create<IRoomPlayerContext>(call =>
            call.Method.Name switch
            {
                "get_RoomObject" => player,
                "get_ObjectId" => player.ObjectId,
                _ => null,
            }
        );

    /// <summary>A booth holding <paramref name="data"/>, with every composer it broadcasts captured
    /// in <paramref name="sent"/>.</summary>
    private static FurnitureClothingChangeLogic Booth(string data, List<IComposer> sent)
    {
        FurnitureDefinitionSnapshot definition = new()
        {
            Id = 1,
            SpriteId = 1,
            Name = "fball_gate",
            ProductType = ProductType.Floor,
            FurniCategory = FurnitureCategory.Default,
            LogicName = "furniture_clothing_change",
            TotalStates = 1,
            Width = 1,
            Length = 1,
            StackHeight = default,
            CanStack = false,
            CanWalk = true,
            CanSit = false,
            CanLay = false,
            CanRecycle = false,
            CanTrade = true,
            CanGroup = false,
            CanSell = true,
            UsagePolicy = FurnitureUsageType.Everybody,
            ExtraData = data,
            StuffDataType = StuffDataType.LegacyKey,
        };

        // ExtraData is a JSON document of named sections, not the raw legacy string -- the booth's
        // pair is written into the stuff data below, the way the room grain writes it.
        IExtraData extraData = new ExtraData(null);

        IRoomFloorItem item = FakeProxy.Create<IRoomFloorItem>(call =>
            call.Method.Name switch
            {
                "get_ExtraData" => extraData,
                "get_Definition" => definition,
                "get_ObjectId" => new RoomObjectId(9),
                _ => null,
            }
        );

        IRoomFloorItemContext ctx = FakeProxy.Create<IRoomFloorItemContext>(call =>
        {
            if (call.Method.Name == "SendComposerToRoomAsync")
            {
                sent.Add((IComposer)call.Args![0]!);

                return Task.CompletedTask;
            }

            return call.Method.Name switch
            {
                "get_Definition" => definition,
                "get_RoomObject" => item,
                "get_ObjectId" => new RoomObjectId(9),
                _ => null,
            };
        });

        FurnitureClothingChangeLogic booth = new(new StuffDataFactory(), ctx);

        booth.StuffData.SetState(data);

        return booth;
    }
}
