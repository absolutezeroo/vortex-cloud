using System;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Vortex.Primitives.Messages.Outgoing.Navigator;
using Vortex.Primitives.Navigator.Enums;
using Vortex.Primitives.Orleans.Snapshots.Navigator;
using Vortex.Primitives.Orleans.Snapshots.Room;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Revisions.Configuration;
using Xunit;
using Rev = Vortex.Revisions.Revision20260701.Revision20260701;

namespace Vortex.Revisions.Tests.Navigator;

/// <summary>
/// The guest-room search result had an <c>object?</c> payload and an empty serializer body. Layout
/// from WIN63's parser (unknowns/_SafePkg_2064/_SafeCls_4150.as) and the DTO it builds
/// (unknowns/_SafePkg_2008/_SafeCls_3104.as): the query, a counted list of rooms in the same block
/// GetGuestRoomResult uses, then a guard and the optional promoted entry.
///
/// The guard is the detail worth pinning. Its sibling, OfficialRooms, asks the same "is there an ad"
/// question with an <i>int</i>; this message uses a boolean. One byte against four, and nothing in
/// either name says so.
/// </summary>
public sealed class GuestRoomSearchResultWireTests
{
    private static readonly Rev Revision = new(Options.Create(new ProtocolLimitsConfig()));

    [Fact]
    public void WritesQueryThenRoomsThenTheAbsentAdGuard()
    {
        ClientPacket body = Serialize(
            new GuestRoomSearchResultMessageComposer
            {
                SearchType = 2,
                SearchParam = "lounge",
                Rooms = [Room(11, "Welcome Lounge")],
                Ad = null,
            }
        );

        body.PopInt().Should().Be(2);
        body.PopString().Should().Be("lounge");
        body.PopInt().Should().Be(1);

        // Start of the room block, which RoomSettingsSerializer owns and OfficialRooms tests cover.
        body.PopInt().Should().Be(11);
        body.PopString().Should().Be("Welcome Lounge");
    }

    /// <summary>
    /// A boolean guard, not an int. Four bytes here would push everything after it out of step for
    /// a client that only reads one.
    /// </summary>
    [Fact]
    public void AbsentAdWritesASingleByteGuard()
    {
        ClientPacket empty = Serialize(
            new GuestRoomSearchResultMessageComposer
            {
                SearchType = 0,
                SearchParam = string.Empty,
                Rooms = [],
                Ad = null,
            }
        );

        empty.PopInt().Should().Be(0);
        empty.PopString().Should().BeEmpty();
        empty.PopInt().Should().Be(0);
        empty.Remaining.Should().Be(1);
        empty.PopBoolean().Should().BeFalse();
    }

    [Fact]
    public void PresentAdWritesTheGuardThenTheEntry()
    {
        ClientPacket body = Serialize(
            new GuestRoomSearchResultMessageComposer
            {
                SearchType = 0,
                SearchParam = string.Empty,
                Rooms = [],
                Ad = new OfficialRoomEntrySnapshot
                {
                    Index = 0,
                    PopupCaption = "Promoted",
                    PopupDescription = "a lounge",
                    ShowDetails = true,
                    PictureText = string.Empty,
                    PictureRef = string.Empty,
                    FolderId = -1,
                    UserCount = 3,
                    Type = OfficialRoomEntryType.Room,
                    Room = Room(11, "Welcome Lounge"),
                },
            }
        );

        body.PopInt().Should().Be(0);
        body.PopString().Should().BeEmpty();
        body.PopInt().Should().Be(0);

        body.PopBoolean().Should().BeTrue();

        body.PopInt().Should().Be(0); // index
        body.PopString().Should().Be("Promoted");
    }

    private static RoomInfoSnapshot Room(int roomId, string name) =>
        new()
        {
            RoomId = roomId,
            Name = name,
            Description = "a lounge",
            OwnerId = (PlayerId)7,
            OwnerName = "absolutezeroo",
            Population = 3,
            DoorMode = RoomDoorModeType.Open,
            PlayersMax = 25,
            TradeType = RoomTradeModeType.Disabled,
            Score = 0,
            Ranking = 0,
            CategoryId = -1,
            Tags = [],
            StaffPick = true,
            AllowBlocking = false,
            AllowPets = false,
            AllowPetsEat = false,
            GroupId = null,
            GroupName = null,
            GroupBadge = null,
            PaintWall = 0,
            PaintFloor = 0,
            PaintLandscape = 0,
            LastUpdatedUtc = DateTime.UnixEpoch,
        };

    private static ClientPacket Serialize(GuestRoomSearchResultMessageComposer composer)
    {
        byte[] bytes = Revision
            .Serializers[typeof(GuestRoomSearchResultMessageComposer)]
            .Serialize(composer)
            .ToArray();

        byte[] payload = new byte[bytes.Length - 6];
        Array.Copy(bytes, 6, payload, 0, payload.Length);

        return new ClientPacket(0, payload);
    }
}
