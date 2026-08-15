using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Triggers;

/// <summary>
/// "The user performs an action" (trigger code 16). Deliberately inert: this client revision ships
/// no configuration class for the code — <c>wired_setup/triggerconfs/</c> has one class per trigger
/// and none of them declares 16 — so the box cannot be given an action to watch for and no wiring
/// here could be reached.
/// </summary>
/// <remarks>
/// It was previously registered as listening for <c>AvatarWalkOnFurniEvent</c>, which is not the
/// event it is about; it never fired only because the base's <c>CanTriggerAsync</c> refuses by
/// default. Declaring nothing is the honest version of the same behaviour: the box exists, and the
/// day the client can configure it, the event and the gate go in together.
/// </remarks>
[RoomObjectLogic("wf_trg_user_performs_action")]
public class WiredTriggerHabboPerformsAction(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredTriggerLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredTriggerType.AVATAR_PERFORMS_ACTION;
}
