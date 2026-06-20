using System;
using FluentAssertions;
using Turbo.Primitives.Groups.Snapshots;
using Turbo.Primitives.Messages.Incoming.Users;
using Turbo.Primitives.Messages.Outgoing.Users;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Packets;
using Xunit;
using Rev = Turbo.Revisions.Revision20260112.Revision20260112;

namespace Turbo.Revisions.Tests.Groups;

/// <summary>
///     Verifies the guild packets serialize / parse with exactly the wire layout the Flash client
///     (vortex-client) expects. These lock the byte contract that the empty stubs lacked.
/// </summary>
public sealed class GuildWireLayoutTests
{
    // Header ids from Revision20260112 Headers.cs (incoming MessageEvent ids).
    private const int CreateGuildEvent = 2792;

    private static readonly Rev Revision = new();

    /// <summary>Writes packet fields the same way the client composer does, for parser input.</summary>
    private static ClientPacket BuildClientPacket(int header, Action<ServerPacket> write)
    {
        ServerPacket sp = new(header);
        write(sp);
        return new ClientPacket(header, sp.ToArray());
    }

    /// <summary>Runs a registered serializer and returns a reader positioned past the 6-byte header.</summary>
    private static ClientPacket SerializeAndReadBody(
        Type composerType,
        IComposer composer
    )
    {
        byte[] bytes = Revision.Serializers[composerType].Serialize(composer).ToArray();
        // AbstractSerializer prepends int length (4) + short header (2).
        byte[] body = new byte[bytes.Length - 6];
        Array.Copy(bytes, 6, body, 0, body.Length);
        return new ClientPacket(0, body);
    }

    [Fact]
    public void CreateGuildParser_ReadsClientLayout()
    {
        // Wire order from CreateGuildMessageComposer.as: name, desc, baseRoomId, primaryColorId,
        // secondaryColorId, badge[] — roomId is 3rd, not the colors.
        ClientPacket packet = BuildClientPacket(
            CreateGuildEvent,
            sp =>
            {
                sp.WriteString("Pixel Painters");
                sp.WriteString("We paint pixels");
                sp.WriteInteger(42); // base room id (3rd — matches client arg order)
                sp.WriteInteger(3); // primary color id
                sp.WriteInteger(7); // secondary color id
                sp.WriteInteger(6); // flattened badge length
                foreach (int v in new[] { 1, 2, 0, 3, 4, 1 })
                {
                    sp.WriteInteger(v);
                }
            }
        );

        IMessageEvent parsed = Revision.Parsers[CreateGuildEvent].Parse(packet);

        CreateGuildMessage? message = parsed.Should().BeOfType<CreateGuildMessage>().Subject;
        message.Name.Should().Be("Pixel Painters");
        message.Description.Should().Be("We paint pixels");
        message.PrimaryColorId.Should().Be(3);
        message.SecondaryColorId.Should().Be(7);
        message.BaseRoomId.Should().Be(42);
        message.BadgeParts.Should().Equal(1, 2, 0, 3, 4, 1);
    }

    [Fact]
    public void HabboGroupDetailsSerializer_WritesClientLayout()
    {
        GroupDetailsSnapshot details = new()
        {
            GroupId = 5,
            IsGuild = true,
            Type = 1,
            Name = "Pixel Painters",
            Description = "desc",
            BadgeCode = "b0102",
            RoomId = 42,
            RoomName = "HQ",
            Status = 1,
            TotalMembers = 9,
            Favourite = false,
            CreationDate = "19-06-2026 04:00",
            IsOwner = true,
            IsAdmin = true,
            OwnerName = "absolutezeroo",
            OpenToJoin = true,
            MembersCanDecorate = true,
            PendingMemberCount = 2,
            HasForum = false
        };

        ClientPacket body = SerializeAndReadBody(
            typeof(HabboGroupDetailsMessageComposer),
            new HabboGroupDetailsMessageComposer { Details = details }
        );

        body.PopInt().Should().Be(5); // groupId
        body.PopBoolean().Should().BeTrue(); // isGuild
        body.PopInt().Should().Be(1); // type
        body.PopString().Should().Be("Pixel Painters");
        body.PopString().Should().Be("desc");
        body.PopString().Should().Be("b0102");
        body.PopInt().Should().Be(42); // roomId
        body.PopString().Should().Be("HQ");
        body.PopInt().Should().Be(1); // status
        body.PopInt().Should().Be(9); // totalMembers
        body.PopBoolean().Should().BeFalse(); // favourite
        body.PopString().Should().Be("19-06-2026 04:00");
        body.PopBoolean().Should().BeTrue(); // isOwner
        body.PopBoolean().Should().BeTrue(); // isAdmin
        body.PopString().Should().Be("absolutezeroo");
        body.PopBoolean().Should().BeTrue(); // openToJoin
        body.PopBoolean().Should().BeTrue(); // membersCanDecorate
        body.PopInt().Should().Be(2); // pendingMemberCount
        body.PopBoolean().Should().BeFalse(); // hasForum
        body.End.Should().BeTrue("the layout must consume the whole packet");
    }

    [Fact]
    public void GuildMembersSerializer_WritesPagedMemberList()
    {
        GroupMembersPageSnapshot page = new()
        {
            GroupId = 5,
            GroupName = "Pixel Painters",
            BaseRoomId = 42,
            BadgeCode = "b0102",
            TotalEntries = 1,
            Members =
            [
                new GroupMemberSnapshot
                {
                    RoleType = 0,
                    UserId = 7,
                    UserName = "absolutezeroo",
                    Figure = "hd-1-1",
                    MemberSince = "19-06-2026"
                }
            ],
            AllowedToManage = true,
            PageSize = 14,
            PageIndex = 0,
            SearchType = 0,
            UserNameFilter = ""
        };

        ClientPacket body = SerializeAndReadBody(
            typeof(GuildMembersMessageComposer),
            new GuildMembersMessageComposer { Page = page }
        );

        body.PopInt().Should().Be(5); // groupId
        body.PopString().Should().Be("Pixel Painters");
        body.PopInt().Should().Be(42); // baseRoomId
        body.PopString().Should().Be("b0102");
        body.PopInt().Should().Be(1); // totalEntries
        body.PopInt().Should().Be(1); // member count
        body.PopInt().Should().Be(0); // roleType
        body.PopInt().Should().Be(7); // userId
        body.PopString().Should().Be("absolutezeroo");
        body.PopString().Should().Be("hd-1-1");
        body.PopString().Should().Be("19-06-2026");
        body.PopBoolean().Should().BeTrue(); // allowedToManage
        body.PopInt().Should().Be(14); // pageSize
        body.PopInt().Should().Be(0); // pageIndex
        body.PopInt().Should().Be(0); // searchType
        body.PopString().Should().Be("");
        body.End.Should().BeTrue();
    }

    [Fact]
    public void GuildCreatedSerializer_WritesRoomAndGroupId()
    {
        ClientPacket body = SerializeAndReadBody(
            typeof(GuildCreatedMessageComposer),
            new GuildCreatedMessageComposer { BaseRoomId = 42, GroupId = 5 }
        );

        body.PopInt().Should().Be(42);
        body.PopInt().Should().Be(5);
        body.End.Should().BeTrue();
    }

    [Fact]
    public void GuildMembershipsSerializer_WritesGuildList()
    {
        GuildMembershipsMessageComposer composer = new()
        {
            Memberships =
            [
                new GuildInfoSnapshot
                {
                    GroupId = 5,
                    GroupName = "Pixel Painters",
                    BadgeCode = "b0102",
                    PrimaryColor = "3",
                    SecondaryColor = "7",
                    Favourite = false,
                    OwnerId = 7,
                    HasForum = true
                }
            ]
        };

        ClientPacket body = SerializeAndReadBody(typeof(GuildMembershipsMessageComposer), composer);

        body.PopInt().Should().Be(1); // count
        body.PopInt().Should().Be(5);
        body.PopString().Should().Be("Pixel Painters");
        body.PopString().Should().Be("b0102");
        body.PopString().Should().Be("3");
        body.PopString().Should().Be("7");
        body.PopBoolean().Should().BeFalse();
        body.PopInt().Should().Be(7);
        body.PopBoolean().Should().BeTrue();
        body.End.Should().BeTrue();
    }
}
