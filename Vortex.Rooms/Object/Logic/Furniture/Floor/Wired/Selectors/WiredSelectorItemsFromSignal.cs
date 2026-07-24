using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Wired;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Selectors;

/// <summary>Outputs the furni carried by the signal that fired this stack (client FurniFromSignal.ts).
/// It simply exposes the SignalItems source, which the context resolves from the signal payload — so it
/// only yields anything inside a receive-signal stack. Was a stub that selected nothing.</summary>
[RoomObjectLogic("wf_slc_furni_signal")]
public class WiredSelectorItemsFromSignal(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredSelectorLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredSelectorType.FURNI_FROM_SIGNAL;

    public override List<WiredFurniSourceType[]> GetAllowedFurniSources() =>
        [[WiredFurniSourceType.SignalItems]];

    public override Task<IWiredSelectionSet> SelectAsync(
        IWiredProcessingContext ctx,
        CancellationToken ct
    ) => ctx.GetWiredSelectionSetAsync(this, ct);
}
