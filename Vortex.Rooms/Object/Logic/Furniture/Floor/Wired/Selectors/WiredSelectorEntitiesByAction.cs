using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Wired;
using Vortex.Rooms.Wired.Rules;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Selectors;

/// <summary>
/// Selects every player in the room currently doing the configured action — the room-wide form of
/// the "user performs action" condition, and the same client <c>WiredUserAction</c> code in int
/// param [0].
/// </summary>
/// <remarks>
/// It was a shell: the box saved its action and selected nobody. The two boxes now answer through
/// one matcher, so a posture that reads one way in a condition cannot read the other way here.
/// </remarks>
[RoomObjectLogic("wf_slc_users_byaction")]
public class WiredSelectorEntitiesByAction(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredSelectorLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredSelectorType.USERS_PERFORMING_ACTION;

    // The dropdown sends the action code; the rule must be declared or the box cannot be saved.
    public override List<IWiredParamRule> GetIntParamRules() => [new WiredParamRule(0)];

    public override Task<IWiredSelectionSet> SelectAsync(
        IWiredProcessingContext ctx,
        CancellationToken ct
    )
    {
        WiredSelectionSet output = new();

        if (_wiredData.IntParams.Count == 0)
        {
            return Task.FromResult<IWiredSelectionSet>(output);
        }

        int actionCode = _wiredData.GetIntParam<int>(0);

        foreach (IRoomAvatar avatar in _ctx.Lookup.Avatars)
        {
            // A selection set is addressed by player id, so bots and pets have nothing to add.
            if (avatar is IRoomPlayer player && WiredUserActionMatcher.Matches(actionCode, player))
            {
                output.SelectedPlayerIds.Add((int)player.PlayerId);
            }
        }

        return Task.FromResult<IWiredSelectionSet>(output);
    }
}
