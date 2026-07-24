using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Wired;
using Vortex.Rooms.Wired.Rules;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Selectors;

/// <summary>
/// Selects everyone in the room of a given kind (client UsersByType.ts). The single int param carries
/// the client's <c>usertype</c> ids — 1 Habbo, 2 Pet, 4 Bot — which are the same numbers as
/// <see cref="RoomObjectType"/>, so it compares against the avatar's own type.
/// <para>
/// A selection set addresses users by player id, and only real Habbos have one, so the Pet and Bot
/// choices resolve to nothing until the set can carry room objects rather than players. Previously
/// this selector was a stub that selected nothing at all, including for Habbos.
/// </para>
/// </summary>
[RoomObjectLogic("wf_slc_users_bytype")]
public class WiredSelectorEntitiesByType(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredSelectorLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredSelectorType.USERS_BY_TYPE;

    public override List<IWiredParamRule> GetIntParamRules() =>
        [new WiredParamRule((int)RoomObjectType.Player)];

    public override Task<IWiredSelectionSet> SelectAsync(
        IWiredProcessingContext ctx,
        CancellationToken ct
    )
    {
        WiredSelectionSet output = new();

        int requiredType =
            _wiredData.IntParams.Count > 0
                ? _wiredData.GetIntParam<int>(0)
                : (int)RoomObjectType.Player;

        foreach (IRoomAvatar avatar in _roomGrain._state.AvatarsByObjectId.Values)
        {
            if ((int)avatar.AvatarType != requiredType || avatar is not IRoomPlayer player)
            {
                continue;
            }

            output.SelectedPlayerIds.Add((int)player.PlayerId);
        }

        return Task.FromResult((IWiredSelectionSet)output);
    }
}
