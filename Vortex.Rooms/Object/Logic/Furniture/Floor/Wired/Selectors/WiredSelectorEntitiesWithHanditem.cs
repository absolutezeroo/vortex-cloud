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

/// <summary>Selects every player in the room holding the configured hand item. Same single-int form
/// as the matching condition (the client shares the dropdown between them), including code 0 meaning
/// empty-handed.</summary>
[RoomObjectLogic("wf_slc_users_handitem")]
public class WiredSelectorEntitiesWithHanditem(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredSelectorLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredSelectorType.USERS_WITH_HANDITEM;

    public override List<IWiredParamRule> GetIntParamRules() =>
        [new WiredRangeParamRule(0, 9999, 0)];

    public override Task<IWiredSelectionSet> SelectAsync(
        IWiredProcessingContext ctx,
        CancellationToken ct
    )
    {
        WiredSelectionSet output = new WiredSelectionSet();
        int required = _wiredData.IntParams.Count > 0 ? _wiredData.GetIntParam<int>(0) : 0;

        foreach (IRoomAvatar avatar in _ctx.Lookup.Avatars)
        {
            if (avatar is not IRoomPlayer roomPlayer || avatar.CarryItemId != required)
            {
                continue;
            }

            output.SelectedPlayerIds.Add((int)roomPlayer.PlayerId);
        }

        return Task.FromResult((IWiredSelectionSet)output);
    }
}
