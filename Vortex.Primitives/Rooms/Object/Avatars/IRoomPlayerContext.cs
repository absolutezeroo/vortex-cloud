using Vortex.Primitives.Rooms.Object.Logic.Avatars;

namespace Vortex.Primitives.Rooms.Object.Avatars;

public interface IRoomPlayerContext
    : IRoomAvatarContext<IRoomPlayer, IRoomPlayerLogic, IRoomPlayerContext>
{
    new IRoomPlayer RoomObject { get; }
}
