using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Action;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Grains;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor;

/// <summary>
/// The welcome gift: one prize per player, and the furniture stays where it is. That is the whole
/// difference from a reward box — a box is consumed by the person who opens it, this one has to
/// survive for the next visitor, so a persisted claim rather than the furniture's absence is what
/// records that someone already took theirs.
/// </summary>
[RoomObjectLogic("furniture_welcome_gift")]
public class FurnitureWelcomeGiftLogic(
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureFloorLogic(stuffDataFactory, ctx)
{
    public override FurnitureUsageType GetUsagePolicy() => FurnitureUsageType.Everybody;

    public override async Task OnUseAsync(ActionContext ctx, int param, CancellationToken ct)
    {
        await _ctx.RoomAs<IRoomCrackable>()
            .ClaimWelcomeGiftAsync(ctx, _ctx.ObjectId, ct)
            .ConfigureAwait(false);
    }
}
