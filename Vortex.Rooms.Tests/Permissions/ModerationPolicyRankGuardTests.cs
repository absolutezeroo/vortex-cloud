using System;
using FluentAssertions;
using Vortex.Primitives.Permissions;
using Xunit;

namespace Vortex.Rooms.Tests.Permissions;

/// <summary>
/// The in-room kick/mute/ban packets used to require a staff capability. The default "player" role
/// is seeded with none, so a room owner could never moderate their own room and the room's
/// WhoCanMute/Kick/Ban settings were unreachable configuration. Room-scoped authority now comes
/// from the room itself; only the relative-rank guard stays at the handler.
/// </summary>
public sealed class ModerationPolicyRankGuardTests
{
    [Fact]
    public void PlainPlayer_HoldsNoStaffCapability()
    {
        // The premise of the bug: an ordinary player passes no capability check at all.
        ModerationPolicy.IsAllowed(PermissionSet.Empty, ModerationAction.Kick).Should().BeFalse();
    }

    [Fact]
    public void PlainPlayer_MayStillActOnAnotherPlainPlayer()
    {
        ModerationPolicy.CanActOnTarget(PermissionSet.Empty, PermissionSet.Empty).Should().BeTrue();
    }

    [Fact]
    public void PlainPlayer_MayNotActOnAModerator()
    {
        PermissionSet moderator = Permissions(Capabilities.Moderation.Kick);

        ModerationPolicy.CanActOnTarget(PermissionSet.Empty, moderator).Should().BeFalse();
    }

    [Fact]
    public void Moderator_MayNotActOnAnAdministrator()
    {
        PermissionSet moderator = Permissions(Capabilities.Moderation.Kick);
        PermissionSet administrator = Permissions(Capabilities.Wildcard);

        ModerationPolicy.CanActOnTarget(moderator, administrator).Should().BeFalse();
    }

    [Fact]
    public void Administrator_MayActOnAModerator()
    {
        PermissionSet administrator = Permissions(Capabilities.Wildcard);
        PermissionSet moderator = Permissions(Capabilities.Moderation.Kick);

        ModerationPolicy.CanActOnTarget(administrator, moderator).Should().BeTrue();
    }

    /// <summary>The bundled overload must stay exactly capability AND rank.</summary>
    [Fact]
    public void BundledOverload_StillRequiresBothCapabilityAndRank()
    {
        PermissionSet moderator = Permissions(Capabilities.Moderation.Kick);
        PermissionSet administrator = Permissions(Capabilities.Wildcard);

        ModerationPolicy
            .IsAllowed(moderator, PermissionSet.Empty, ModerationAction.Kick)
            .Should()
            .BeTrue();

        ModerationPolicy
            .IsAllowed(moderator, administrator, ModerationAction.Kick)
            .Should()
            .BeFalse();

        ModerationPolicy
            .IsAllowed(PermissionSet.Empty, PermissionSet.Empty, ModerationAction.Kick)
            .Should()
            .BeFalse();
    }

    private static PermissionSet Permissions(params string[] capabilities) =>
        new(Array.Empty<string>(), capabilities);
}
