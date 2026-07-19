using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Actions;

/// <summary>Furnidata alias of <see cref="WiredActionSetFurniAltitude"/> for the "lower furni" box.
/// Same SET_FURNI_ALTITUDE setup UI and int params as the base (the operator lives in param [1]); only
/// the furnidata logic key differs, and the attribute is not inherited, so each variant must declare
/// its own key.</summary>
[RoomObjectLogic("wf_act_lower_furni")]
public class WiredActionLowerFurni(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : WiredActionSetFurniAltitude(grainFactory, stuffDataFactory, ctx) { }
