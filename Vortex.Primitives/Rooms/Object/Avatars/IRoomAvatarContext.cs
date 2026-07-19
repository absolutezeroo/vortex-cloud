using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Rooms.Object.Logic.Avatars;
using Vortex.Primitives.Rooms.Snapshots.Mapping;

namespace Vortex.Primitives.Rooms.Object.Avatars;

public interface IRoomAvatarContext<out TObject, out TLogic, out TSelf>
    : IRoomObjectContext<TObject, TLogic, TSelf>,
        IRoomAvatarContext
    where TObject : IRoomAvatar<TObject, TLogic, TSelf>
    where TSelf : IRoomAvatarContext<TObject, TLogic, TSelf>
    where TLogic : IRoomAvatarLogic<TObject, TLogic, TSelf>
{
    new TObject RoomObject { get; }
}

public interface IRoomAvatarContext : IRoomObjectContext
{
    new IRoomAvatar RoomObject { get; }
    public Task<RoomTileSnapshot> GetTileSnapshotAsync(CancellationToken ct);
}
