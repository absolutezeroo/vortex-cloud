using System;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Action;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor;

/// <summary>
/// The spin result must be picked server-side — trusting a client-supplied outcome would let a
/// modified client always land on a favorable state.
/// </summary>
// Registered under the client's logic name, not the furniture's classname. The logic string travels
// to the client (VortexFurniDefinitionMessageComposer writes it), which resolves it against
// RoomObjectLogicEnum -- and that enum knows "furniture_habbowheel". "wheel_of_fortune" is the
// classname in furnidata, so a definition carrying it as its logic matched this class server-side
// while the client fell back to default logic and animated nothing.
[RoomObjectLogic("furniture_habbowheel")]
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
