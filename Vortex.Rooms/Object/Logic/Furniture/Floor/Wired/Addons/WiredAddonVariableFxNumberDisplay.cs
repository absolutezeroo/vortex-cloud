using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Addons;

/// <summary>The variable's value drawn as a number rather than a bar.</summary>
/// <remarks>
/// One of the six Variable FX displays. Everything but the code and the category lives in
/// <see cref="FurnitureWiredVariableFxAddonLogic"/>, exactly as the client splits them.
/// </remarks>
[RoomObjectLogic("wf_xtra_fx_number_display")]
public class WiredAddonVariableFxNumberDisplay(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredVariableFxAddonLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredAddonType.VARIABLE_FX_NUMBER_DISPLAY;

    protected override int CategoryId => 5;
}
