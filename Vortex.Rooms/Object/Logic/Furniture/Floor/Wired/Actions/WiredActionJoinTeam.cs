using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Games;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Wired.Rules;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Actions;

/// <summary>
/// Adds each resolved user to a game team (Habbo's "join team"). Client JoinTeam.ts sends
/// <c>[team, type]</c>: the team is 1-4 (matching <see cref="GameTeamColor"/>), and the type picks
/// which game the team belongs to — <c>team_type.0</c> Wired, <c>.1</c> Battle Banzai, <c>.2</c>
/// Freeze. Delegates to <see cref="Grains.Systems.RoomGameSystem"/> so team state has one owner.
/// <para>
/// The type was previously read as an auto-balance flag, which does not exist in the client: picking
/// Battle Banzai silently put the user in the smallest team rather than the one chosen, and Freeze was
/// out of range so the box could not be saved at all. Only the Wired team subsystem is modelled today,
/// so all three types share it; the parameter is honoured to the extent it can be.
/// </para>
/// </summary>
[RoomObjectLogic("wf_act_join_team")]
public class WiredActionJoinTeam(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredActionLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredActionType.JOIN_TEAM;

    public override List<IWiredParamRule> GetIntParamRules() =>
        [
            new WiredEnumParamRule<GameTeamColor>(
                GameTeamColor.Red,
                GameTeamColor.Red,
                GameTeamColor.Green,
                GameTeamColor.Blue,
                GameTeamColor.Yellow
            ),
            new WiredRangeParamRule(0, 2, 0), // team type: 0 Wired, 1 Battle Banzai, 2 Freeze
        ];

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
        GameTeamColor chosenTeam = _wiredData.GetIntParam<GameTeamColor>(0);

        IWiredSelectionSet selection = await ctx.GetEffectiveSelectionAsync(this, ct);

        foreach (int playerId in selection.SelectedPlayerIds)
        {
            try
            {
                await _roomGrain.GameSystem.JoinTeamAsync(playerId, chosenTeam, ct);
            }
            catch
            {
                continue;
            }
        }

        return true;
    }
}
