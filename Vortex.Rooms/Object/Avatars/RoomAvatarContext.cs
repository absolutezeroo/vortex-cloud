using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Primitives.Rooms.Object.Logic.Avatars;
using Vortex.Primitives.Rooms.Snapshots.Mapping;
using Vortex.Rooms.Grains;

namespace Vortex.Rooms.Object.Avatars;

public abstract class RoomAvatarContext<TObject, TLogic, TSelf>(
    RoomGrain roomGrain,
    TObject roomObject
)
    : RoomObjectContext<TObject, TLogic, TSelf>(roomGrain, roomObject),
        IRoomAvatarContext<TObject, TLogic, TSelf>
    where TObject : IRoomAvatar<TObject, TLogic, TSelf>
    where TSelf : IRoomAvatarContext<TObject, TLogic, TSelf>
    where TLogic : IRoomAvatarLogic<TObject, TLogic, TSelf>
{
    IRoomAvatar IRoomAvatarContext.RoomObject => RoomObject;

    public virtual Task<RoomTileSnapshot> GetTileSnapshotAsync(CancellationToken ct) =>
        _roomGrain.GetTileSnapshotAsync(RoomObject.X, RoomObject.Y, ct);
}
