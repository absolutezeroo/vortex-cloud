using System;
using System.Threading;
using System.Threading.Tasks;
using Turbo.Primitives.Action;
using Turbo.Primitives.Furniture.Enums;
using Turbo.Primitives.Furniture.Providers;
using Turbo.Primitives.Rooms.Object.Furniture.Floor;
using Turbo.Primitives.Rooms.Object.Logic;
using Turbo.Primitives.Rooms.Object.Logic.Furniture;

namespace Turbo.Rooms.Object.Logic.Furniture.Floor;

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
