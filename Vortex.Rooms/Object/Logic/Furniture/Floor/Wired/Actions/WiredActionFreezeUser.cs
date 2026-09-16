using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Wired.Rules;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Actions;

/// <summary>
/// Roots each resolved user where they stand (Habbo's "freeze user", <c>wf_act_freeze_habbo</c>,
/// client action code 31). The lock is the same one a Freeze-game hit uses, held on
/// the avatar itself, and it stays until a <see cref="WiredActionUnfreezeUser"/> box (or a Freeze
/// thaw) releases it — leaving the room releases it with the avatar.
/// </summary>
[RoomObjectLogic("wf_act_freeze_habbo")]
public class WiredActionFreezeUser(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredActionLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredActionType.FREEZE_USER;

    /// <summary>
    /// Two, from the client's own form: <c>FreezeUser.ts</c> returns
    /// <c>[effectDropdown.selectedId, cancelCheckbox ? 1 : 0]</c>, and its dropdown is built over
    /// 0..4 inclusive. This class used to declare none and to say so in its summary, which is the
    /// claim the client contradicts -- so TryNormalizeIntParams refused the whole update and the
    /// box could never be saved, even though ExecuteAsync below works.
    /// </summary>
    /// <remarks>
    /// Declaring the rules makes the box saveable. HONOURING the two values is separate work:
    /// ExecuteAsync still ignores both, freezing without the chosen effect and without releasing
    /// on teleport. That is a gap in behaviour, not in configuration, and it is left visible here
    /// rather than half-implemented.
    /// </remarks>
    public override List<IWiredParamRule> GetIntParamRules() =>
        [new WiredRangeParamRule(0, 4, 0), new WiredBoolParamRule(false)];

    public override List<WiredPlayerSourceType[]> GetAllowedPlayerSources() =>
        [
            [
                WiredPlayerSourceType.TriggeredUser,
                WiredPlayerSourceType.SelectorUsers,
                WiredPlayerSourceType.SignalUsers,
            ],
        ];

    public override async Task<bool> ExecuteAsync(IWiredExecutionContext ctx, CancellationToken ct)
    {
        IWiredSelectionSet selection = await ctx.GetEffectiveSelectionAsync(this, ct);

        foreach (int playerId in selection.SelectedPlayerIds)
        {
            _ctx.Game.LockMovement(playerId);
        }

        return true;
    }
}
