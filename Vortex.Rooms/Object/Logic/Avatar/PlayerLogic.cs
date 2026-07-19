using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Object.Logic.Avatars;

namespace Vortex.Rooms.Object.Logic.Avatar;

[RoomObjectLogic("default_avatar")]
public sealed class PlayerLogic(IRoomPlayerContext ctx)
    : AvatarLogic<IRoomPlayer, IRoomPlayerLogic, IRoomPlayerContext>(ctx),
        IRoomPlayerLogic { }
