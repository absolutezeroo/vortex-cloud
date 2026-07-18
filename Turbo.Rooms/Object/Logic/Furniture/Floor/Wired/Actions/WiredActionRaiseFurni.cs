using Orleans;
using Turbo.Primitives.Furniture.Providers;
using Turbo.Primitives.Rooms.Object.Furniture.Floor;
using Turbo.Primitives.Rooms.Object.Logic;

namespace Turbo.Rooms.Object.Logic.Furniture.Floor.Wired.Actions;

/// <summary>Furnidata alias of <see cref="WiredActionSetFurniAltitude"/> for the "raise furni" box.
/// It shares the exact SET_FURNI_ALTITUDE setup UI and int params (the operator still lives in param
/// [1]); only the furnidata logic key differs, and the attribute is not inherited, so each variant
/// must declare its own key.</summary>
[RoomObjectLogic("wf_act_raise_furni")]
public class WiredActionRaiseFurni(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : WiredActionSetFurniAltitude(grainFactory, stuffDataFactory, ctx) { }
