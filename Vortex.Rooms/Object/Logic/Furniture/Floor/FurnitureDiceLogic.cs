using System;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Action;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Object.Logic.Furniture;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor;

/// <summary>
/// State 0 is "off" (no face showing); states 1..TotalStates-1 are die faces. The roll outcome must
/// be picked server-side — trusting a client-supplied face would let a modified client always throw
/// a favorable result.
/// </summary>
[RoomObjectLogic("dice")]
public class FurnitureDiceLogic(IStuffDataFactory stuffDataFactory, IRoomFloorItemContext ctx)
    : FurnitureFloorLogic(stuffDataFactory, ctx)
{
    private const int OffState = 0;

    protected override StuffPersistanceType _stuffPersistanceType =>
        StuffPersistanceType.RoomActive;

    public override async Task OnUseAsync(ActionContext ctx, int param, CancellationToken ct)
    {
        int totalStates = _ctx.Definition.TotalStates;

        if (param == FurnitureDiceAction.TurnOff || totalStates <= 1)
        {
            await SetStateAsync(OffState);
            return;
        }

        await SetStateAsync(Random.Shared.Next(1, totalStates));
    }
}
