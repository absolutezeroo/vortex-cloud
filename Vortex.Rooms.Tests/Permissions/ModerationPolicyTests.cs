using System;
using FluentAssertions;
using Vortex.Primitives.Permissions;
using Xunit;

namespace Vortex.Rooms.Tests.Permissions;

public sealed class ModerationPolicyTests
{
    [Theory]
    [InlineData(ModerationAction.Kick, Capabilities.Moderation.Kick)]
    [InlineData(ModerationAction.Mute, Capabilities.Moderation.Mute)]
    [InlineData(ModerationAction.Ban, Capabilities.Moderation.Ban)]
    [InlineData(ModerationAction.Alert, Capabilities.Moderation.Alert)]
    [InlineData(ModerationAction.TradingLock, Capabilities.Moderation.TradingLock)]
    public void SpecificCapability_AllowsMatchingAction(ModerationAction action, string capability)
    {
        PermissionSet permissions = Permissions(capability);

        bool allowed = ModerationPolicy.IsAllowed(permissions, action);

        allowed.Should().BeTrue();
    }

    [Theory]
    [InlineData(ModerationAction.Kick)]
    [InlineData(ModerationAction.Mute)]
    [InlineData(ModerationAction.Ban)]
    [InlineData(ModerationAction.Alert)]
    [InlineData(ModerationAction.TradingLock)]
    public void ModerateAny_AllowsEveryAction(ModerationAction action)
    {
        PermissionSet permissions = Permissions(Capabilities.Room.ModerateAny);

        bool allowed = ModerationPolicy.IsAllowed(permissions, action);

        allowed.Should().BeTrue();
    }

    [Theory]
    [InlineData(ModerationAction.Kick)]
    [InlineData(ModerationAction.Mute)]
    [InlineData(ModerationAction.Ban)]
    [InlineData(ModerationAction.Alert)]
    [InlineData(ModerationAction.TradingLock)]
    public void Wildcard_AllowsEveryAction(ModerationAction action)
    {
        PermissionSet permissions = Permissions(Capabilities.Wildcard);

        bool allowed = ModerationPolicy.IsAllowed(permissions, action);

        allowed.Should().BeTrue();
    }

    [Theory]
    [InlineData(ModerationAction.Kick)]
    [InlineData(ModerationAction.Mute)]
    [InlineData(ModerationAction.Ban)]
    [InlineData(ModerationAction.Alert)]
    [InlineData(ModerationAction.TradingLock)]
    public void NoCapability_DeniesAction(ModerationAction action)
    {
        bool allowed = ModerationPolicy.IsAllowed(PermissionSet.Empty, action);

        allowed.Should().BeFalse();
    }

    [Fact]
    public void TargetAwareOverload_AllowsActingOnLowerRankedTarget()
    {
        PermissionSet moderator = Permissions(Capabilities.Moderation.Kick);
        PermissionSet normalPlayer = PermissionSet.Empty;

        bool allowed = ModerationPolicy.IsAllowed(moderator, normalPlayer, ModerationAction.Kick);

        allowed.Should().BeTrue();
    }

    [Fact]
    public void TargetAwareOverload_AllowsActingOnEqualRankedTarget()
    {
        PermissionSet moderator = Permissions(Capabilities.Moderation.Kick);
        PermissionSet otherModerator = Permissions(Capabilities.Moderation.Kick);

        bool allowed = ModerationPolicy.IsAllowed(moderator, otherModerator, ModerationAction.Kick);

        allowed.Should().BeTrue();
    }

    [Fact]
    public void TargetAwareOverload_DeniesActingOnHigherRankedTarget()
    {
        PermissionSet moderator = Permissions(Capabilities.Moderation.Kick);
        PermissionSet administrator = Permissions(Capabilities.Wildcard);

        bool allowed = ModerationPolicy.IsAllowed(moderator, administrator, ModerationAction.Kick);

        allowed.Should().BeFalse();
    }

    [Fact]
    public void TargetAwareOverload_SuperUserCanActOnAnyRank()
    {
        PermissionSet administrator = Permissions(Capabilities.Wildcard);
        PermissionSet otherAdministrator = Permissions(Capabilities.Wildcard);

        bool allowed = ModerationPolicy.IsAllowed(
            administrator,
            otherAdministrator,
            ModerationAction.Ban
        );

        allowed.Should().BeTrue();
    }

    [Fact]
    public void TargetAwareOverload_StillDeniesWithoutCapability()
    {
        bool allowed = ModerationPolicy.IsAllowed(
            PermissionSet.Empty,
            PermissionSet.Empty,
            ModerationAction.Kick
        );

        allowed.Should().BeFalse();
    }

    private static PermissionSet Permissions(params string[] capabilities)
    {
        return new PermissionSet(Array.Empty<string>(), capabilities);
    }
}
