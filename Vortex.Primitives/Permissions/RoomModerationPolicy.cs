using Vortex.Primitives.Navigator.Enums;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.Primitives.Permissions;

/// <summary>
/// Pure mapping from a subject's resolved <see cref="RoomControllerType"/> and the room's
/// per-action <see cref="ModSettingType"/> to "may they mute/kick/ban here?". These settings were
/// persisted and echoed to the client but enforced nowhere, which left every occupant able to mute,
/// kick and ban anyone in the room.
/// </summary>
public static class RoomModerationPolicy
{
    /// <param name="level">The subject's standing inside this room.</param>
    /// <param name="setting">The room's policy for this action.</param>
    /// <param name="hasStaffModerationCapability">
    /// Whether the subject holds the staff capability for the action being attempted. Passed in
    /// separately rather than folded into <paramref name="level"/> on purpose: moderation authority
    /// and building authority are orthogonal, and <see cref="RoomControllerType"/> is a single
    /// linear ladder. Elevating a moderator on that ladder would also hand them build rights in
    /// every room, while leaving it out entirely would deny a staff role that holds
    /// <c>moderation.kick</c> but not <c>room.moderate.any</c> — a combination an administrator can
    /// create, since roles are editable.
    /// </param>
    public static bool CanModerate(
        RoomControllerType level,
        ModSettingType setting,
        bool hasStaffModerationCapability = false
    )
    {
        // Staff capability and room ownership both override the per-room setting; that is what
        // keeps "owner only" workable for staff intervention.
        if (hasStaffModerationCapability || level >= RoomControllerType.Owner)
        {
            return true;
        }

        return setting switch
        {
            ModSettingType.Owner => false,
            // "All users" — the only setting that grants a plain occupant moderation authority.
            // The client offers it on the kick dropdown alone; we honour it wherever it arrives
            // rather than second-guessing which action it came from.
            ModSettingType.All => true,
            ModSettingType.Rights or ModSettingType.RightsOrGroup => level
                >= RoomControllerType.Rights,
            // Guild-only: a plain rights-holder is deliberately excluded, so the level has to come
            // from guild standing (GroupRights / GroupAdmin).
            ModSettingType.GroupRights => level >= RoomControllerType.GroupRights,
            _ => false,
        };
    }
}
