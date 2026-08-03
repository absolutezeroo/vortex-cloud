using System;
using FluentAssertions;
using Vortex.Database.Entities.Groups;
using Vortex.Database.Entities.Players;
using Vortex.Players.Groups;
using Vortex.Primitives.Groups;
using Vortex.Primitives.Groups.Enums;
using Vortex.Primitives.Groups.Snapshots;
using Vortex.Primitives.Players.Enums;
using Vortex.Primitives.Rooms.Enums;
using Xunit;

namespace Vortex.Players.Tests.Groups;

/// <summary>
/// The member list is one of the few places where the server hands the client a number it switches
/// on. Building the snapshot by hand at five call sites is how the owner check ended up missing from
/// some of them — the owner is stored as an ordinary member row, so forgetting it reports the guild
/// founder as a plain member and the client then offers to kick them.
/// </summary>
public sealed class GroupMemberMapperTests
{
    private const int OwnerId = 7;

    [Fact]
    public void TheOwnerOutranksTheirStoredRank()
    {
        GroupMemberSnapshot snapshot = GroupMemberMapper.FromMember(
            Member(OwnerId, GroupMemberRank.Member),
            OwnerId
        );

        snapshot.RoleType.Should().Be(GroupMemberRoles.Owner);
    }

    [Fact]
    public void AnAdminIsReportedAsAdmin() =>
        GroupMemberMapper
            .FromMember(Member(42, GroupMemberRank.Admin), OwnerId)
            .RoleType.Should()
            .Be(GroupMemberRoles.Admin);

    [Fact]
    public void APlainMemberIsReportedAsMember() =>
        GroupMemberMapper
            .FromMember(Member(42, GroupMemberRank.Member), OwnerId)
            .RoleType.Should()
            .Be(GroupMemberRoles.Member);

    [Fact]
    public void APendingRequestIsReportedAsRequested()
    {
        GroupMembershipRequestEntity request = new()
        {
            GroupEntityId = 1,
            PlayerEntityId = 42,
            GroupEntity = null!,
            PlayerEntity = Player(42),
            CreatedAt = new DateTime(2026, 3, 9, 14, 0, 0, DateTimeKind.Utc),
        };

        GroupMemberSnapshot snapshot = GroupMemberMapper.FromRequest(request);

        snapshot.RoleType.Should().Be(GroupMemberRoles.Requested);
        snapshot.UserId.Should().Be(42);
        snapshot.MemberSince.Should().Be("09-03-2026");
    }

    [Fact]
    public void TheJoinDateIsDayFirst()
    {
        // The client displays this verbatim; an ISO or month-first date silently shows the wrong day
        // for the first twelve days of every month.
        GroupMemberEntity member = Member(42, GroupMemberRank.Member);
        member.CreatedAt = new DateTime(2026, 3, 9, 14, 0, 0, DateTimeKind.Utc);

        GroupMemberMapper.FromMember(member, OwnerId).MemberSince.Should().Be("09-03-2026");
    }

    [Fact]
    public void ANewlyAddedMemberCarriesTheSuppliedTimestamp()
    {
        GroupMemberSnapshot snapshot = GroupMemberMapper.NewMember(
            Player(42),
            new DateTime(2026, 12, 25, 0, 0, 0, DateTimeKind.Utc)
        );

        snapshot.RoleType.Should().Be(GroupMemberRoles.Member);
        snapshot.UserId.Should().Be(42);
        snapshot.UserName.Should().Be("player-42");
        snapshot.MemberSince.Should().Be("25-12-2026");
    }

    [Fact]
    public void ANewlyFiledRequestIsReportedAsRequested() =>
        GroupMemberMapper
            .NewRequest(Player(42), new DateTime(2026, 12, 25, 0, 0, 0, DateTimeKind.Utc))
            .RoleType.Should()
            .Be(GroupMemberRoles.Requested);

    private static GroupMemberEntity Member(int playerId, GroupMemberRank rank) =>
        new()
        {
            GroupEntityId = 1,
            PlayerEntityId = playerId,
            Rank = rank,
            GroupEntity = null!,
            PlayerEntity = Player(playerId),
        };

    private static PlayerEntity Player(int id) =>
        new()
        {
            Id = id,
            Name = $"player-{id}",
            Figure = "hd-180-1",
            Gender = AvatarGenderType.Male,
            PlayerStatus = PlayerStatusType.Offline,
            PlayerPerks = PlayerPerkFlags.None,
        };
}
