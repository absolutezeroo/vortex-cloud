using System;
using FluentAssertions;
using Turbo.Primitives.Permissions;
using Xunit;

namespace Turbo.Rooms.Tests.Permissions;

public sealed class ModerationPolicyTests
{
    [Theory]
    [InlineData(ModerationAction.Kick, Capabilities.Moderation.Kick)]
    [InlineData(ModerationAction.Mute, Capabilities.Moderation.Mute)]
    [InlineData(ModerationAction.Ban, Capabilities.Moderation.Ban)]
    [InlineData(ModerationAction.Alert, Capabilities.Moderation.Alert)]
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
    public void NoCapability_DeniesAction(ModerationAction action)
    {
        bool allowed = ModerationPolicy.IsAllowed(PermissionSet.Empty, action);

        allowed.Should().BeFalse();
    }

    private static PermissionSet Permissions(params string[] capabilities)
    {
        return new PermissionSet(Array.Empty<string>(), capabilities);
    }
}
