using Vortex.Primitives.Players.Enums;

namespace Vortex.Primitives.Permissions;

/// <summary>The distinct moderation actions a subject can be authorized for.</summary>
public enum ModerationAction
{
    Kick,
    Mute,
    Ban,
    Alert,
    TradingLock,
}

/// <summary>
/// Capability gate for moderation actions. Each action is granted by its specific capability or by
/// the room-wide <see cref="Capabilities.Room.ModerateAny"/> (and, implicitly, by
/// <see cref="Capabilities.Wildcard"/>). This is the single server-side authorization choke point for
/// moderation; the client security level is only a UI hint.
/// </summary>
public static class ModerationPolicy
{
    /// <summary>Pure capability check, no relative-rank comparison. Use the target-aware overload
    /// when a specific player is being acted on — this one alone lets a lower-ranked staff member
    /// sanction a higher-ranked one.</summary>
    public static bool IsAllowed(PermissionSet permissions, ModerationAction action)
    {
        string capability = RequiredCapability(action);

        return permissions.HasAny(capability, Capabilities.Room.ModerateAny);
    }

    /// <summary>Capability check plus a relative-rank guard: staff can act on a target of equal or
    /// lower <see cref="SecurityLevelType"/>, never a higher one — otherwise a Moderator could kick
    /// an Administrator.</summary>
    public static bool IsAllowed(
        PermissionSet actorPermissions,
        PermissionSet targetPermissions,
        ModerationAction action
    )
    {
        if (!IsAllowed(actorPermissions, action))
        {
            return false;
        }

        SecurityLevelType actorLevel = SecurityLevelPolicy.Resolve(actorPermissions);
        SecurityLevelType targetLevel = SecurityLevelPolicy.Resolve(targetPermissions);

        return actorLevel >= targetLevel;
    }

    public static string RequiredCapability(ModerationAction action) =>
        action switch
        {
            ModerationAction.Kick => Capabilities.Moderation.Kick,
            ModerationAction.Mute => Capabilities.Moderation.Mute,
            ModerationAction.Ban => Capabilities.Moderation.Ban,
            ModerationAction.Alert => Capabilities.Moderation.Alert,
            ModerationAction.TradingLock => Capabilities.Moderation.TradingLock,
            _ => Capabilities.Wildcard,
        };
}
