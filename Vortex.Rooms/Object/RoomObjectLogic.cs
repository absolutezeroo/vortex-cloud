using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Action;
using Vortex.Primitives.Rooms.Events.RoomObject;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Rooms.Grains;

namespace Vortex.Rooms.Object;

public abstract class RoomObjectLogic<TObject, TSelf, TContext>(TContext ctx)
    : IRoomObjectLogic<TObject, TSelf, TContext>
    where TObject : IRoomObject<TObject, TSelf, TContext>
    where TContext : IRoomObjectContext<TObject, TSelf, TContext>
    where TSelf : IRoomObjectLogic<TObject, TSelf, TContext>
{
    protected readonly TContext _ctx = ctx;
    protected readonly RoomGrain _roomGrain = (RoomGrain)ctx.Room;

    public TContext Context => _ctx;

    IRoomObjectContext IRoomObjectLogic.Context => Context;

    public virtual Task OnAttachAsync(CancellationToken ct) =>
        _ctx.PublishRoomEventAsync(
            new RoomObjectAttatchedEvent
            {
                RoomId = _ctx.RoomId,
                CausedBy = ActionContext.CreateForSystem(_ctx.RoomId),
                ObjectId = _ctx.ObjectId,
            },
            ct
        );

    public virtual Task OnDetachAsync(CancellationToken ct) =>
        _ctx.PublishRoomEventAsync(
            new RoomObjectDetatchedEvent
            {
                RoomId = _ctx.RoomId,
                CausedBy = ActionContext.CreateForSystem(_ctx.RoomId),
                ObjectId = _ctx.ObjectId,
            },
            ct
        );
}
