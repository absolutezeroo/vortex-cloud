using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Action;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Events.Avatar;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Object.Logic.Furniture;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor;

[RoomObjectLogic("default_floor")]
public class FurnitureFloorLogic(IStuffDataFactory stuffDataFactory, IRoomFloorItemContext ctx)
    : FurnitureLogic<IRoomFloorItem, IFurnitureFloorLogic, IRoomFloorItemContext>(
        stuffDataFactory,
        ctx
    ),
        IFurnitureFloorLogic
{
    IRoomFloorItemContext IFurnitureFloorLogic.Context => Context;

    public virtual bool CanStack() => _ctx.Definition.CanStack;

    public virtual bool CanWalk() => _ctx.Definition.CanWalk;

    public virtual bool CanSit() => _ctx.Definition.CanSit;

    public virtual bool CanLay() => _ctx.Definition.CanLay;

    public override bool CanRoll() => true;

    public virtual Altitude GetPostureOffset()
    {
        if (CanSit())
        {
            return GetStackHeight();
        }

        if (CanLay())
        {
            return GetStackHeight();
        }

        return Altitude.Zero;
    }

    public override Altitude GetStackHeight() => _ctx.Definition.StackHeight;

    public override Task OnStateChangedAsync(CancellationToken ct)
    {
        _ctx.RefreshTile();

        return base.OnStateChangedAsync(ct);
    }

    public virtual Task OnInvokeAsync(IRoomAvatarContext ctx, CancellationToken ct) =>
        Task.CompletedTask;

    public virtual Task OnWalkOnAsync(IRoomAvatarContext ctx, CancellationToken ct) =>
        _ctx.PublishRoomEventAsync(
            new AvatarWalkOnFurniEvent
            {
                RoomId = _ctx.RoomId,
                CausedBy = ActionContext.CreateForObjectContext(ctx),
                ObjectId = ctx.ObjectId,
                FurniId = _ctx.ObjectId,
            },
            ct
        );

    public virtual Task OnWalkOffAsync(IRoomAvatarContext ctx, CancellationToken ct) =>
        _ctx.PublishRoomEventAsync(
            new AvatarWalkOffFurniEvent
            {
                RoomId = _ctx.RoomId,
                CausedBy = ActionContext.CreateForObjectContext(ctx),
                ObjectId = ctx.ObjectId,
                FurniId = _ctx.ObjectId,
            },
            ct
        );
}
