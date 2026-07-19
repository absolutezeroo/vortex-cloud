using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Primitives.Rooms.Object.Logic.Avatars;
using Vortex.Rooms.Grains;

namespace Vortex.Rooms.Object.Avatars.Player;

public sealed class RoomPlayerContext(RoomGrain roomGrain, IRoomPlayer roomObject)
    : RoomAvatarContext<IRoomPlayer, IRoomPlayerLogic, IRoomPlayerContext>(roomGrain, roomObject),
        IRoomPlayerContext
{
    IRoomPlayer IRoomPlayerContext.RoomObject => RoomObject;
}
