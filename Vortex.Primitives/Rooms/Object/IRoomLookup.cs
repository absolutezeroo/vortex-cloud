using System.Collections.Generic;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Primitives.Rooms.Object.Furniture;

namespace Vortex.Primitives.Rooms.Object;

/// <summary>
/// Finding the other objects in the room. This is not a grain contract: room-object logic runs
/// inside the room's own turn, and these resolve against live state with no hop and no await --
/// which is exactly why nothing here returns a <c>Task</c>.
/// </summary>
public interface IRoomLookup
{
    /// <summary>The item with this object id, or null if the room has no such item.</summary>
    IRoomItem? FindItem(RoomObjectId objectId);

    /// <summary>The avatar with this object id, or null if the room has no such avatar.</summary>
    IRoomAvatar? FindAvatar(RoomObjectId objectId);

    /// <summary>The avatar this player is currently embodying, or null if they are not here.</summary>
    IRoomAvatar? FindAvatar(PlayerId playerId);

    /// <summary>Every item in the room. Live view -- do not hold it across an await.</summary>
    IReadOnlyCollection<IRoomItem> Items { get; }

    /// <summary>Every avatar in the room, players and pets alike. Live view.</summary>
    IReadOnlyCollection<IRoomAvatar> Avatars { get; }

    /// <summary>How many players are in the room.</summary>
    int AvatarCount { get; }
}
