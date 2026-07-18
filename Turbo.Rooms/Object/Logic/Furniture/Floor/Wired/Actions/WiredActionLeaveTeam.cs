using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Primitives.Furniture.Providers;
using Turbo.Primitives.Rooms.Enums.Wired;
using Turbo.Primitives.Rooms.Object.Furniture.Floor;
using Turbo.Primitives.Rooms.Object.Logic;
using Turbo.Primitives.Rooms.Wired;

namespace Turbo.Rooms.Object.Logic.Furniture.Floor.Wired.Actions;

/// <summary>Removes each resolved user from their game team (Habbo's "leave team"), clearing their team
/// aura. No int params (actiontypes/class_3685). Delegates to <see cref="Grains.Systems.RoomGameSystem"/>.</summary>
[RoomObjectLogic("wf_act_leave_team")]
public class WiredActionLeaveTeam(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredActionLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredActionType.LEAVE_TEAM;

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
            try
            {
                await _roomGrain.GameSystem.LeaveTeamAsync(playerId, ct);
            }
            catch
            {
                continue;
            }
        }

        return true;
    }
}
