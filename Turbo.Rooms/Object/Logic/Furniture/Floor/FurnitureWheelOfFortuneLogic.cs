using System;
using System.Threading;
using System.Threading.Tasks;
using Turbo.Primitives.Action;
using Turbo.Primitives.Furniture.Enums;
using Turbo.Primitives.Furniture.Providers;
using Turbo.Primitives.Rooms.Object.Furniture.Floor;
using Turbo.Primitives.Rooms.Object.Logic;

namespace Turbo.Rooms.Object.Logic.Furniture.Floor;

/// <summary>
/// The spin result must be picked server-side — trusting a client-supplied outcome would let a
/// modified client always land on a favorable state.
/// </summary>
[RoomObjectLogic("wheel_of_fortune")]
public class FurnitureWheelOfFortuneLogic(
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureFloorLogic(stuffDataFactory, ctx)
{
    protected override StuffPersistanceType _stuffPersistanceType =>
        StuffPersistanceType.RoomActive;

    public override async Task OnUseAsync(ActionContext ctx, int param, CancellationToken ct)
    {
        int totalStates = _ctx.Definition.TotalStates;

        if (totalStates <= 1)
        {
            return;
        }

        await SetStateAsync(Random.Shared.Next(0, totalStates));
    }
}
