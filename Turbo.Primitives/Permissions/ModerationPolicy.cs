namespace Turbo.Primitives.Permissions;

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
/// Pure capability gate for moderation actions. Each action is granted by its specific capability or by
/// the room-wide <see cref="Capabilities.Room.ModerateAny"/> (and, implicitly, by
/// <see cref="Capabilities.Wildcard"/>). This is the single server-side authorization choke point for
/// moderation; the client security level is only a UI hint.
/// </summary>
public static class ModerationPolicy
{
    public static bool IsAllowed(PermissionSet permissions, ModerationAction action)
    {
        string capability = RequiredCapability(action);

        return permissions.HasAny(capability, Capabilities.Room.ModerateAny);
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
