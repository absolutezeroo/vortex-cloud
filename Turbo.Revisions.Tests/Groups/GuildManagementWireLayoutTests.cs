using System.Collections.Generic;
using FluentAssertions;
using Turbo.Primitives.Groups.Snapshots;
using Turbo.Primitives.Messages.Incoming.Users;
using Turbo.Primitives.Messages.Outgoing.Users;
using Turbo.Primitives.Packets;
using Xunit;
using Rev = Turbo.Revisions.Revision20260112.Revision20260112;

namespace Turbo.Revisions.Tests.Groups;

/// <summary>Wire-layout lock for the slice-2 guild management packets (parsers + serializers).</summary>
public sealed class GuildManagementWireLayoutTests
{
    // Incoming MessageEvent ids (Headers.cs).
    private const int UpdateGuildBadgeEvent = 3099;
    private const int UpdateGuildSettingsEvent = 3356;
    private const int KickMemberEvent = 380;

    private static readonly Rev Revision = new();

    private static ClientPacket BuildClientPacket(int header, System.Action<ServerPacket> write)
    {
        ServerPacket sp = new ServerPacket(header);
        write(sp);
        return new ClientPacket(header, sp.ToArray());
    }

    private static ClientPacket SerializeAndReadBody(
        System.Type composerType,
        Turbo.Primitives.Networking.IComposer composer
    )
    {
        byte[] bytes = Revision.Serializers[composerType].Serialize(composer).ToArray();
        byte[] body = new byte[bytes.Length - 6];
        System.Array.Copy(bytes, 6, body, 0, body.Length);
        return new ClientPacket(0, body);
    }

    [Fact]
    public void UpdateGuildBadgeParser_ReadsFlattenedParts()
    {
        ClientPacket packet = BuildClientPacket(
            UpdateGuildBadgeEvent,
            sp =>
            {
                sp.WriteInteger(5); // groupId
                sp.WriteInteger(6); // flattened length
                foreach (int v in new[] { 1, 2, 0, 3, 4, 1 })
                    sp.WriteInteger(v);
            }
        );

        UpdateGuildBadgeMessage? msg = Revision
            .Parsers[UpdateGuildBadgeEvent]
            .Parse(packet)
            .Should()
            .BeOfType<UpdateGuildBadgeMessage>()
            .Subject;

        msg.GroupId.Should().Be(5);
        msg.BadgeParts.Should().Equal(1, 2, 0, 3, 4, 1);
    }

    [Fact]
    public void UpdateGuildSettingsParser_ReadsTypeAndRights()
    {
        ClientPacket packet = BuildClientPacket(
            UpdateGuildSettingsEvent,
            sp =>
            {
                sp.WriteInteger(5); // groupId
                sp.WriteInteger(1); // guildType (exclusive)
                sp.WriteInteger(1); // rightsLevel
            }
        );

        UpdateGuildSettingsMessage? msg = Revision
            .Parsers[UpdateGuildSettingsEvent]
            .Parse(packet)
            .Should()
            .BeOfType<UpdateGuildSettingsMessage>()
            .Subject;

        msg.GroupId.Should().Be(5);
        msg.GuildType.Should().Be(1);
        msg.RightsLevel.Should().Be(1);
    }

    [Fact]
    public void KickMemberParser_ReadsBlockFlag()
    {
        ClientPacket packet = BuildClientPacket(
            KickMemberEvent,
            sp =>
            {
                sp.WriteInteger(5); // groupId
                sp.WriteInteger(7); // userId
                sp.WriteBoolean(true); // block
            }
        );

        KickMemberMessage? msg = Revision
            .Parsers[KickMemberEvent]
            .Parse(packet)
            .Should()
            .BeOfType<KickMemberMessage>()
            .Subject;

        msg.GroupId.Should().Be(5);
        msg.UserId.Should().Be(7);
        msg.Block.Should().BeTrue();
    }

    [Fact]
    public void GuildEditInfoSerializer_WritesClientLayout()
    {
        GroupEditInfoSnapshot info = new GroupEditInfoSnapshot
        {
            OwnedRooms =
            [
                new GroupRoomSnapshot
                {
                    RoomId = 42,
                    RoomName = "HQ",
                    HasControllers = true,
                },
            ],
            IsOwner = true,
            GroupId = 5,
            GroupName = "Pixel Painters",
            GroupDescription = "desc",
            BaseRoomId = 42,
            PrimaryColorId = 3,
            SecondaryColorId = 7,
            GuildType = 1,
            GuildRightsLevel = 0,
            Locked = false,
            Url = "",
            BadgeParts = [],
            BadgeCode = "b0102",
            MembershipCount = 9,
        };

        ClientPacket body = SerializeAndReadBody(
            typeof(GuildEditInfoMessageComposer),
            new GuildEditInfoMessageComposer { Info = info }
        );

        body.PopInt().Should().Be(1); // owned room count
        body.PopInt().Should().Be(42);
        body.PopString().Should().Be("HQ");
        body.PopBoolean().Should().BeTrue();
        body.PopBoolean().Should().BeTrue(); // isOwner
        body.PopInt().Should().Be(5); // groupId
        body.PopString().Should().Be("Pixel Painters");
        body.PopString().Should().Be("desc");
        body.PopInt().Should().Be(42); // baseRoomId
        body.PopInt().Should().Be(3); // primary
        body.PopInt().Should().Be(7); // secondary
        body.PopInt().Should().Be(1); // type
        body.PopInt().Should().Be(0); // rights
        body.PopBoolean().Should().BeFalse(); // locked
        body.PopString().Should().Be(""); // url
        body.PopInt().Should().Be(0); // badge parts count
        body.PopString().Should().Be("b0102");
        body.PopInt().Should().Be(9); // membershipCount
        body.End.Should().BeTrue();
    }

    [Fact]
    public void GuildMembershipUpdatedSerializer_WritesMember()
    {
        ClientPacket body = SerializeAndReadBody(
            typeof(GuildMembershipUpdatedMessageComposer),
            new GuildMembershipUpdatedMessageComposer
            {
                GroupId = 5,
                Member = new GroupMemberSnapshot
                {
                    RoleType = 2,
                    UserId = 7,
                    UserName = "bob",
                    Figure = "hd-1-1",
                    MemberSince = "19-06-2026",
                },
            }
        );

        body.PopInt().Should().Be(5); // groupId
        body.PopInt().Should().Be(2); // roleType
        body.PopInt().Should().Be(7); // userId
        body.PopString().Should().Be("bob");
        body.PopString().Should().Be("hd-1-1");
        body.PopString().Should().Be("19-06-2026");
        body.End.Should().BeTrue();
    }

    [Fact]
    public void HabboGroupDeactivatedSerializer_WritesGroupId()
    {
        ClientPacket body = SerializeAndReadBody(
            typeof(HabboGroupDeactivatedMessageComposer),
            new HabboGroupDeactivatedMessageComposer { GroupId = 5 }
        );

        body.PopInt().Should().Be(5);
        body.End.Should().BeTrue();
    }
}
