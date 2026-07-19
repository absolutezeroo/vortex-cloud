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

/// <summary>Adds each resolved user to a game team (Habbo's "join team"). Int params from the client
/// setup form (actiontypes/class_3938): [0] = team (1-4, matching <see cref="GameTeamColor"/>),
/// [1] = mode (0 = the chosen team, 1 = auto-balance into the currently smallest team). Delegates to
/// <see cref="Grains.Systems.RoomGameSystem"/> so team state has one owner; the balanced mode
/// recomputes the smallest team per user so several joiners distribute evenly.</summary>
[RoomObjectLogic("wf_act_join_team")]
public class WiredActionJoinTeam(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredActionLogic(grainFactory, stuffDataFactory, ctx)
{
    private const int ModeBalanced = 1;

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
            new WiredRangeParamRule(0, 1, 0), // mode: 0 = chosen team, 1 = balanced
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
        bool balanced =
            _wiredData.IntParams.Count > 1 && _wiredData.GetIntParam<int>(1) == ModeBalanced;

        IWiredSelectionSet selection = await ctx.GetEffectiveSelectionAsync(this, ct);

        foreach (int playerId in selection.SelectedPlayerIds)
        {
            try
            {
                GameTeamColor team = balanced
                    ? _roomGrain.GameSystem.GetSmallestTeam()
                    : chosenTeam;

                await _roomGrain.GameSystem.JoinTeamAsync(playerId, team, ct);
            }
            catch
            {
                continue;
            }
        }

        return true;
    }
}
