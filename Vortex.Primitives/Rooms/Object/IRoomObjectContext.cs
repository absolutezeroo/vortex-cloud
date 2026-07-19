using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms.Events;
using Vortex.Primitives.Rooms.Grains;
using Vortex.Primitives.Rooms.Object.Logic;

namespace Vortex.Primitives.Rooms.Object;

public interface IRoomObjectContext<out TObject, out TLogic, out TSelf> : IRoomObjectContext
    where TObject : IRoomObject<TObject, TLogic, TSelf>
    where TSelf : IRoomObjectContext<TObject, TLogic, TSelf>
    where TLogic : IRoomObjectLogic<TObject, TLogic, TSelf>
{
    new TObject RoomObject { get; }
}

public interface IRoomObjectContext
{
    public RoomId RoomId { get; }
    public IRoomGrain Room { get; }

    public RoomObjectId ObjectId { get; }
    public IRoomObject RoomObject { get; }

    public Task PublishRoomEventAsync(RoomEvent evt, CancellationToken ct);
    public Task SendComposerToRoomAsync(IComposer composer);
}
