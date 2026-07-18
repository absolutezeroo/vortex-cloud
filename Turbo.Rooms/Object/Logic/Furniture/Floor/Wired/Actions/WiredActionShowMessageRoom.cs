using Orleans;
using Turbo.Primitives.Furniture.Providers;
using Turbo.Primitives.Rooms.Object.Furniture.Floor;
using Turbo.Primitives.Rooms.Object.Logic;

namespace Turbo.Rooms.Object.Logic.Furniture.Floor.Wired.Actions;

/// <summary>Furnidata alias of <see cref="WiredActionShowMessage"/> for the room-wide "show message"
/// box. Same CHAT setup UI and params (visibility still lives in param [0]); only the furnidata logic
/// key differs, and the attribute is not inherited, so each variant must declare its own key.</summary>
[RoomObjectLogic("wf_act_show_message_room")]
public class WiredActionShowMessageRoom(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : WiredActionShowMessage(grainFactory, stuffDataFactory, ctx) { }
