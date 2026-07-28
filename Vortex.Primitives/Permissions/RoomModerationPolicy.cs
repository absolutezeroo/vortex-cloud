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
    public static bool CanModerate(RoomControllerType level, ModSettingType setting)
    {
        // Staff moderation capability and room ownership override the per-room setting; that is
        // what makes "owner only" still workable for staff intervention.
        if (level >= RoomControllerType.Owner)
        {
            return true;
        }

        return setting switch
        {
            ModSettingType.Owner => false,
            ModSettingType.Rights or ModSettingType.RightsOrGroup => level
                >= RoomControllerType.Rights,
            // Guild-only: a plain rights-holder is deliberately excluded, so the level has to come
            // from guild standing (GroupRights / GroupAdmin).
            ModSettingType.GroupRights => level >= RoomControllerType.GroupRights,
            _ => false,
        };
    }
}
