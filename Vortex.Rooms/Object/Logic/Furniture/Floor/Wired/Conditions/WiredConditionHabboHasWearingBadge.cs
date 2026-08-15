using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Wired;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Conditions;

/// <summary>
/// "The user is wearing the badge". The client's form is a single free-text field holding the badge
/// code (the box has no int params at all), and it asks about a worn badge — one of the five profile
/// slots — not merely an owned one. An empty code matches nothing, which is what an unconfigured box
/// should do.
/// </summary>
[RoomObjectLogic("wf_cnd_habbo_owns_badge")]
public class WiredConditionHabboHasWearingBadge(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredConditionLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredConditionType.ACTOR_IS_WEARING_BADGE;

    public override async Task PrepareAsync(IWiredProcessingContext ctx, CancellationToken ct)
    {
        PlayerId triggerer = ctx.Event.CausedBy.PlayerId;

        if (triggerer > 0 && !string.IsNullOrWhiteSpace(_wiredData.StringParam))
        {
            await _ctx.Furni.EnsureWornBadgesAsync(triggerer, ct);
        }
    }

    public override bool Evaluate(IWiredProcessingContext ctx)
    {
        PlayerId triggerer = ctx.Event.CausedBy.PlayerId;
        bool result = false;

        if (triggerer > 0)
        {
            result = _ctx.Furni.IsWearingBadge(triggerer, _wiredData.StringParam.Trim());
        }

        return IsNegative() ? !result : result;
    }
}
