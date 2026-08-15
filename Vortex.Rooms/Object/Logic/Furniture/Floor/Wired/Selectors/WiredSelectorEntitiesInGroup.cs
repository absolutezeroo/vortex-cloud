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

/// <summary>Selects every player in the room who belongs to the configured guild. Shares the
/// condition's two-option form (UsersInGroup.as): empty string param = the room's own guild, a
/// decimal id = one of the configuring player's guilds.</summary>
[RoomObjectLogic("wf_slc_users_group")]
public class WiredSelectorEntitiesInGroup(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredSelectorLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredSelectorType.USERS_IN_GROUP;

    public override async Task<IWiredSelectionSet> SelectAsync(
        IWiredProcessingContext ctx,
        CancellationToken ct
    )
    {
        WiredSelectionSet output = new WiredSelectionSet();

        if (WiredGroupTarget.Resolve(_wiredData.StringParam, _ctx.GroupId) is not int groupId)
        {
            return output;
        }

        // One roster load for the whole room, not one membership query per avatar.
        await _ctx.Furni.EnsureGuildRosterAsync(groupId, ct);

        foreach (IRoomAvatar avatar in _ctx.Lookup.Avatars)
        {
            if (
                avatar is not IRoomPlayer roomPlayer
                || !_ctx.Furni.IsGuildMember(groupId, roomPlayer.PlayerId)
            )
            {
                continue;
            }

            output.SelectedPlayerIds.Add((int)roomPlayer.PlayerId);
        }

        return output;
    }
}
