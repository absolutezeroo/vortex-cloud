using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms;
using Vortex.Primitives.Rooms.Events;
using Vortex.Primitives.Rooms.Grains;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Rooms.Grains;

namespace Vortex.Rooms.Object;

public abstract class RoomObjectContext<TObject, TLogic, TSelf>(
    RoomGrain roomGrain,
    TObject roomObject
) : IRoomObjectContext<TObject, TLogic, TSelf>
    where TObject : IRoomObject<TObject, TLogic, TSelf>
    where TSelf : IRoomObjectContext<TObject, TLogic, TSelf>
    where TLogic : IRoomObjectLogic<TObject, TLogic, TSelf>
{
    protected readonly RoomGrain _roomGrain = roomGrain;
    protected readonly TObject _roomObject = roomObject;

    public RoomId RoomId => _roomGrain._state.RoomId;
    public IRoomGrain Room => _roomGrain;

    public RoomObjectId ObjectId => _roomObject.ObjectId;
    public TObject RoomObject => _roomObject;

    IRoomObject IRoomObjectContext.RoomObject => RoomObject;

    public Task PublishRoomEventAsync(RoomEvent evt, CancellationToken ct) =>
        _roomGrain.PublishRoomEventAsync(evt, ct);

    public Task SendComposerToRoomAsync(IComposer composer) =>
        _roomGrain.SendComposerToRoomAsync(composer);
}
