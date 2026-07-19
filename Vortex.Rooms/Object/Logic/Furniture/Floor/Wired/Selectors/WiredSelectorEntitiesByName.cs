using System.Collections.Generic;
using System.Linq;
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

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Selectors;

[RoomObjectLogic("wf_slc_users_byname")]
public class WiredSelectorEntitiesByName(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredSelectorLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredSelectorType.USERS_BY_NAME;

    public override Task<IWiredSelectionSet> SelectAsync(
        IWiredProcessingContext ctx,
        CancellationToken ct
    )
    {
        WiredSelectionSet output = new WiredSelectionSet();
        HashSet<string> names = _wiredData
            .StringParam.Split('/')
            .Select(n => n.Trim().ToLower())
            .ToHashSet();

        foreach (IRoomAvatar avatar in _roomGrain._state.AvatarsByObjectId.Values)
        {
            if (avatar is not IRoomPlayer roomPlayer || !names.Contains(roomPlayer.Name.ToLower()))
            {
                continue;
            }

            output.SelectedPlayerIds.Add((int)roomPlayer.PlayerId);
        }

        return Task.FromResult((IWiredSelectionSet)output);
    }
}
