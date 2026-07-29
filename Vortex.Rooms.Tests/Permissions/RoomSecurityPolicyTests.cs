using System;
using FluentAssertions;
using Vortex.Primitives.Action;
using Vortex.Primitives.Permissions;
using Vortex.Primitives.Rooms.Enums;
using Xunit;

namespace Vortex.Rooms.Tests.Permissions;

public sealed class RoomSecurityPolicyTests
{
    [Fact]
    public void SystemOrigin_ResolvesModerator()
    {
        RoomControllerType level = RoomSecurityPolicy.ResolveControllerLevel(
            ActionOrigin.System,
            PermissionSet.Empty,
            isExplicitOwner: false,
            hasExplicitRights: false
        );

        level.Should().Be(RoomControllerType.Moderator);
    }

    [Fact]
    public void WildcardSuperUser_ResolvesModerator()
    {
        PermissionSet permissions = Permissions(Capabilities.Wildcard);

        RoomControllerType level = RoomSecurityPolicy.ResolveControllerLevel(
            ActionOrigin.Player,
            permissions,
            isExplicitOwner: false,
            hasExplicitRights: false
        );

        level.Should().Be(RoomControllerType.Moderator);
    }

    [Fact]
    public void ModerateAny_ResolvesModerator()
    {
        PermissionSet permissions = Permissions(Capabilities.Room.ModerateAny);

        RoomControllerType level = RoomSecurityPolicy.ResolveControllerLevel(
            ActionOrigin.Player,
            permissions,
            isExplicitOwner: false,
            hasExplicitRights: false
        );

        level.Should().Be(RoomControllerType.Moderator);
    }

    [Fact]
    public void BuildAny_ResolvesOwner()
    {
        PermissionSet permissions = Permissions(Capabilities.Room.BuildAny);

        RoomControllerType level = RoomSecurityPolicy.ResolveControllerLevel(
            ActionOrigin.Player,
            permissions,
            isExplicitOwner: false,
            hasExplicitRights: false
        );

        level.Should().Be(RoomControllerType.Owner);
    }

    [Fact]
    public void ExplicitOwner_ResolvesOwner()
    {
        RoomControllerType level = RoomSecurityPolicy.ResolveControllerLevel(
            ActionOrigin.Player,
            PermissionSet.Empty,
            isExplicitOwner: true,
            hasExplicitRights: false
        );

        level.Should().Be(RoomControllerType.Owner);
    }

    [Fact]
    public void ExplicitRights_ResolvesRights()
    {
        RoomControllerType level = RoomSecurityPolicy.ResolveControllerLevel(
            ActionOrigin.Player,
            PermissionSet.Empty,
            isExplicitOwner: false,
            hasExplicitRights: true
        );

        level.Should().Be(RoomControllerType.Rights);
    }

    [Fact]
    public void NoGrant_ResolvesNone()
    {
        RoomControllerType level = RoomSecurityPolicy.ResolveControllerLevel(
            ActionOrigin.Player,
            PermissionSet.Empty,
            isExplicitOwner: false,
            hasExplicitRights: false
        );

        level.Should().Be(RoomControllerType.None);
    }

    [Fact]
    public void GuildAdmin_ResolvesGroupAdmin()
    {
        RoomControllerType level = RoomSecurityPolicy.ResolveControllerLevel(
            ActionOrigin.Player,
            PermissionSet.Empty,
            isExplicitOwner: false,
            hasExplicitRights: false,
            new RoomGroupContext(IsMember: true, IsAdmin: true, MembersCanDecorate: false)
        );

        level.Should().Be(RoomControllerType.GroupAdmin);
    }

    [Fact]
    public void GuildMember_WhenMembersMayDecorate_ResolvesGroupRights()
    {
        RoomControllerType level = RoomSecurityPolicy.ResolveControllerLevel(
            ActionOrigin.Player,
            PermissionSet.Empty,
            isExplicitOwner: false,
            hasExplicitRights: false,
            new RoomGroupContext(IsMember: true, IsAdmin: false, MembersCanDecorate: true)
        );

        level.Should().Be(RoomControllerType.GroupRights);
    }

    [Fact]
    public void GuildMember_WhenDecorationIsAdminOnly_ResolvesNone()
    {
        RoomControllerType level = RoomSecurityPolicy.ResolveControllerLevel(
            ActionOrigin.Player,
            PermissionSet.Empty,
            isExplicitOwner: false,
            hasExplicitRights: false,
            new RoomGroupContext(IsMember: true, IsAdmin: false, MembersCanDecorate: false)
        );

        level.Should().Be(RoomControllerType.None);
    }

    /// <summary>
    /// Regression: a guild room used to require GroupAdmin for every build action, a level nothing
    /// could produce, so classic rights-holders silently lost their build rights the moment a guild
    /// was created on the room.
    /// </summary>
    [Fact]
    public void ExplicitRights_InGuildRoom_KeepsAtLeastRights()
    {
        RoomControllerType level = RoomSecurityPolicy.ResolveControllerLevel(
            ActionOrigin.Player,
            PermissionSet.Empty,
            isExplicitOwner: false,
            hasExplicitRights: true,
            new RoomGroupContext(IsMember: false, IsAdmin: false, MembersCanDecorate: true)
        );

        level.Should().Be(RoomControllerType.Rights);
        (level >= RoomControllerType.Rights).Should().BeTrue();
    }

    [Fact]
    public void GuildMemberWithExplicitRights_PrefersTheHigherGroupLevel()
    {
        RoomControllerType level = RoomSecurityPolicy.ResolveControllerLevel(
            ActionOrigin.Player,
            PermissionSet.Empty,
            isExplicitOwner: false,
            hasExplicitRights: true,
            new RoomGroupContext(IsMember: true, IsAdmin: false, MembersCanDecorate: true)
        );

        level.Should().Be(RoomControllerType.GroupRights);
    }

    /// <summary>Staff and ownership must still outrank any guild standing.</summary>
    [Fact]
    public void ExplicitOwner_OutranksGuildAdmin()
    {
        RoomControllerType level = RoomSecurityPolicy.ResolveControllerLevel(
            ActionOrigin.Player,
            PermissionSet.Empty,
            isExplicitOwner: true,
            hasExplicitRights: false,
            new RoomGroupContext(IsMember: true, IsAdmin: true, MembersCanDecorate: true)
        );

        level.Should().Be(RoomControllerType.Owner);
    }

    /// <summary>
    /// Pins a deliberate divergence: a moderator sits ABOVE Owner on the controller ladder, yet is
    /// not room-administration authority. Two similar-sounding questions, two correct answers —
    /// locked here so nobody "harmonises" them by accident.
    /// </summary>
    [Fact]
    public void Moderator_OutranksOwnerOnTheLadder_ButMayNotAdministerTheRoom()
    {
        PermissionSet moderator = Permissions(Capabilities.Room.ModerateAny);

        RoomControllerType level = RoomSecurityPolicy.ResolveControllerLevel(
            ActionOrigin.Player,
            moderator,
            isExplicitOwner: false,
            hasExplicitRights: false
        );

        (level >= RoomControllerType.Owner).Should().BeTrue();
        RoomSecurityPolicy.IsRoomOwner(moderator, isExplicitOwner: false).Should().BeFalse();
    }

    [Fact]
    public void BuildAny_IsRoomAdministrationAuthority()
    {
        PermissionSet builder = Permissions(Capabilities.Room.BuildAny);

        RoomSecurityPolicy.IsRoomOwner(builder, isExplicitOwner: false).Should().BeTrue();
    }

    private static PermissionSet Permissions(params string[] capabilities)
    {
        return new PermissionSet(Array.Empty<string>(), capabilities);
    }
}
