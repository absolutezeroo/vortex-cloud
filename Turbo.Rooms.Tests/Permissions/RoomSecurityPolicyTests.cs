using System;
using FluentAssertions;
using Turbo.Primitives.Action;
using Turbo.Primitives.Permissions;
using Turbo.Primitives.Rooms.Enums;
using Xunit;

namespace Turbo.Rooms.Tests.Permissions;

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

    private static PermissionSet Permissions(params string[] capabilities)
    {
        return new PermissionSet(Array.Empty<string>(), capabilities);
    }
}
