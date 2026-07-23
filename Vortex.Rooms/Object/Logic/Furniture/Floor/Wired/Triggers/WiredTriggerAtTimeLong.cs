using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Triggers;

/// <summary>
/// The long "at given time" trigger (<c>wf_trg_at_time_long</c>). It is an alternate furni for the same
/// wired box: the client drives both through the one conf code (TRIGGER_ONCE = 3), so the slider, its
/// 1..1200 pulse range and the 500ms-per-pulse unit are identical — only the furni differs. Arcturus
/// does the same (WiredTriggerAtTimeLong is a copy of WiredTriggerAtSetTime, both AT_GIVEN_TIME), so the
/// behaviour is inherited wholesale rather than re-specified.
/// </summary>
[RoomObjectLogic("wf_trg_at_time_long")]
public class WiredTriggerAtTimeLong(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : WiredTriggerAtTime(grainFactory, stuffDataFactory, ctx);
