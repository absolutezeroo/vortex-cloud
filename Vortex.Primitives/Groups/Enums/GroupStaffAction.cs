namespace Vortex.Primitives.Groups.Enums;

/// <summary>
/// What a hotel operator can do to one player's standing in a guild, from outside it.
/// </summary>
/// <remarks>
/// Same reasoning as <see cref="ForumStaffAction"/>: every one of these exists for a guild admin
/// already and is gated on being one. The operator's version skips the rank check and nothing else,
/// so the row written, the event published and the base-room notification are identical -- a
/// membership change that skipped that notification would leave an ex-member standing in the guild
/// room with the build rights it granted.
/// </remarks>
public enum GroupStaffAction
{
    ApproveRequest = 0,
    RejectRequest = 1,
    Kick = 2,
    KickAndBlock = 3,
    LiftBan = 4,
}
