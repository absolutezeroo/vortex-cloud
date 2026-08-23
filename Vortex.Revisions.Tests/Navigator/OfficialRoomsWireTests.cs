using System;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Vortex.Primitives.Navigator.Enums;
using Vortex.Primitives.Orleans.Snapshots.Navigator;
using Vortex.Primitives.Orleans.Snapshots.Room;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Protocol.Messages.Outgoing.Navigator;
using Vortex.Revisions.Configuration;
using Xunit;
using Rev = Vortex.Revisions.Revision20260701.Revision20260701;

namespace Vortex.Revisions.Tests.Navigator;

/// <summary>
/// The public-rooms composer had an empty serializer body and <c>object?</c> payload fields, so the
/// panel received a header and nothing else. The layout asserted here is the client's own read
/// order: entries, an int guard for the optional ad entry, then the promoted groups.
/// </summary>
public sealed class OfficialRoomsWireTests
{
    private static readonly Rev Revision = new(Options.Create(new ProtocolLimitsConfig()));

    [Fact]
    public void RoomEntry_WritesItsFixedPrefixThenTheRoomBlock()
    {
        ClientPacket body = Serialize(
            new OfficialRoomsMessageComposer
            {
                Entries = [RoomEntry(index: 0, Room(11, "Welcome Lounge"))],
                AdRoom = null,
                PromotedRooms = [],
            }
        );

        body.PopInt().Should().Be(1); // entry count
        body.PopInt().Should().Be(0); // index
        body.PopString().Should().Be("Welcome Lounge"); // popupCaption
        body.PopString().Should().Be("a lounge"); // popupDesc
        body.PopInt().Should().Be(1); // showDetails, an int on the wire and not a boolean
        body.PopString().Should().BeEmpty(); // picText
        body.PopString().Should().BeEmpty(); // picRef
        body.PopInt().Should().Be(-1); // folderId
        body.PopInt().Should().Be(3); // userCount
        body.PopInt().Should().Be((int)OfficialRoomEntryType.Room);
        body.PopInt().Should().Be(11); // room block starts: roomId
    }

    /// <summary>Absent ad room must still write its guard, otherwise the client reads the promoted
    /// count out of the middle of an entry.</summary>
    [Fact]
    public void NoAdRoom_WritesTheGuardAndSkipsTheEntry()
    {
        ClientPacket body = Serialize(
            new OfficialRoomsMessageComposer
            {
                Entries = [],
                AdRoom = null,
                PromotedRooms = [],
            }
        );

        body.PopInt().Should().Be(0); // entry count
        body.PopInt().Should().Be(0); // ad-room guard
        body.PopInt().Should().Be(0); // promoted group count
        body.End.Should().BeTrue();
    }

    [Fact]
    public void AdRoom_WritesTheGuardThenAFullEntry()
    {
        ClientPacket body = Serialize(
            new OfficialRoomsMessageComposer
            {
                Entries = [],
                AdRoom = RoomEntry(index: 7, Room(99, "Sponsored")),
                PromotedRooms = [],
            }
        );

        body.PopInt().Should().Be(0); // entry count
        body.PopInt().Should().Be(1); // ad-room guard
        body.PopInt().Should().Be(7); // index
        body.PopString().Should().Be("Sponsored");
    }

    /// <summary>
    /// A "room" entry whose room is missing cannot be written as one -- the client would block on a
    /// room block that never arrives. The serializer demotes it to a folder entry so the stream
    /// stays readable.
    /// </summary>
    [Fact]
    public void RoomEntryWithoutARoom_IsDemotedToAFolderEntry()
    {
        ClientPacket body = Serialize(
            new OfficialRoomsMessageComposer
            {
                Entries = [RoomEntry(index: 0, room: null)],
                AdRoom = null,
                PromotedRooms = [],
            }
        );

        SkipEntryPrefix(body);

        body.PopInt().Should().Be((int)OfficialRoomEntryType.Folder);
        body.PopBoolean().Should().BeFalse(); // the folder's open flag, not a room block
        body.PopInt().Should().Be(0); // ad-room guard
        body.PopInt().Should().Be(0); // promoted group count
        body.End.Should().BeTrue();
    }

    [Fact]
    public void TagEntry_WritesTheTagInsteadOfARoomBlock()
    {
        ClientPacket body = Serialize(
            new OfficialRoomsMessageComposer
            {
                Entries =
                [
                    RoomEntry(index: 0, room: null) with
                    {
                        Type = OfficialRoomEntryType.Tag,
                        Tag = "party",
                    },
                ],
                AdRoom = null,
                PromotedRooms = [],
            }
        );

        SkipEntryPrefix(body);

        body.PopInt().Should().Be((int)OfficialRoomEntryType.Tag);
        body.PopString().Should().Be("party");
        body.End.Should().BeFalse();
    }

    [Fact]
    public void PromotedGroup_WritesCodeFigureCountThenItsRooms()
    {
        ClientPacket body = Serialize(
            new OfficialRoomsMessageComposer
            {
                Entries = [],
                AdRoom = null,
                PromotedRooms =
                [
                    new PromotedRoomCategorySnapshot
                    {
                        Code = "partyRoom",
                        LeaderFigure = "hd-180-1",
                        Rooms = [Room(21, "Dance Floor")],
                    },
                ],
            }
        );

        body.PopInt().Should().Be(0); // entry count
        body.PopInt().Should().Be(0); // ad-room guard
        body.PopInt().Should().Be(1); // promoted group count
        body.PopString().Should().Be("partyRoom");
        body.PopString().Should().Be("hd-180-1");
        body.PopInt().Should().Be(1); // room count
        body.PopInt().Should().Be(21); // room block starts
    }

    private static void SkipEntryPrefix(ClientPacket body)
    {
        body.PopInt(); // entry count
        body.PopInt(); // index
        body.PopString(); // popupCaption
        body.PopString(); // popupDesc
        body.PopInt(); // showDetails
        body.PopString(); // picText
        body.PopString(); // picRef
        body.PopInt(); // folderId
        body.PopInt(); // userCount
    }

    private static OfficialRoomEntrySnapshot RoomEntry(int index, RoomInfoSnapshot? room) =>
        new()
        {
            Index = index,
            PopupCaption = room?.Name ?? string.Empty,
            PopupDescription = "a lounge",
            ShowDetails = true,
            PictureText = string.Empty,
            PictureRef = string.Empty,
            FolderId = -1,
            UserCount = 3,
            Type = OfficialRoomEntryType.Room,
            Room = room,
        };

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
            PaintWall = string.Empty,
            PaintFloor = string.Empty,
            PaintLandscape = string.Empty,
            LastUpdatedUtc = DateTime.UnixEpoch,
        };

    private static ClientPacket Serialize(OfficialRoomsMessageComposer composer)
    {
        byte[] bytes = Revision
            .Serializers[typeof(OfficialRoomsMessageComposer)]
            .Serialize(composer)
            .ToArray();

        byte[] payload = new byte[bytes.Length - 6];
        Array.Copy(bytes, 6, payload, 0, payload.Length);

        return new ClientPacket(0, payload);
    }
}
