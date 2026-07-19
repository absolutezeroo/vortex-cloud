using Vortex.Primitives.Action;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.Primitives.Permissions;

/// <summary>
/// Pure mapping from a resolved <see cref="PermissionSet"/> (plus a subject's explicit ownership and
/// room-rights) to an effective <see cref="RoomControllerType"/>. Staff capabilities elevate the level
/// across every room: <see cref="Capabilities.Wildcard"/> and <see cref="Capabilities.Room.ModerateAny"/>
/// reach moderator everywhere, while <see cref="Capabilities.Room.BuildAny"/> reaches owner so build/pickup
/// checks pass regardless of the room owner. The classic per-room rights fallback is preserved.
/// </summary>
public static class RoomSecurityPolicy
{
    public static RoomControllerType ResolveControllerLevel(
        ActionOrigin origin,
        PermissionSet permissions,
        bool isExplicitOwner,
        bool hasExplicitRights
    )
    {
        if (origin == ActionOrigin.System)
        {
            return RoomControllerType.Moderator;
        }

        if (permissions.IsSuperUser || permissions.Has(Capabilities.Room.ModerateAny))
        {
            return RoomControllerType.Moderator;
        }

        if (isExplicitOwner || permissions.Has(Capabilities.Room.BuildAny))
        {
            return RoomControllerType.Owner;
        }

        if (hasExplicitRights)
        {
            return RoomControllerType.Rights;
        }

        return RoomControllerType.None;
    }

    public static bool IsRoomOwner(PermissionSet permissions, bool isExplicitOwner)
    {
        if (isExplicitOwner)
        {
            return true;
        }

        return permissions.Has(Capabilities.Room.BuildAny);
    }
}
