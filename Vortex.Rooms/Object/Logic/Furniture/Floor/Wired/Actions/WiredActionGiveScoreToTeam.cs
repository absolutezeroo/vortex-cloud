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

/// <summary>Awards points to a fixed team regardless of who triggered (Habbo's "give points to
/// predefined team"). Int params (actiontypes/class_3921, which extends GiveScore): [0] = points
/// (signed), [1] = times-per-game cap (0 = unlimited), [2] = team (1-4, matching
/// <see cref="GameTeamColor"/>). The cap is enforced per score box. Delegates to
/// <see cref="Grains.Systems.RoomGameSystem"/>.</summary>
[RoomObjectLogic("wf_act_give_score_tm")]
public class WiredActionGiveScoreToTeam(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredActionLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredActionType.GIVE_SCORE_TO_PREDEFINED_TEAM;

    public override List<IWiredParamRule> GetIntParamRules() =>
        [
            new WiredRangeParamRule(-1000, 1000, 1), // points (signed)
            new WiredRangeParamRule(0, 10, 0), // times-per-game cap (0 = unlimited)
            new WiredEnumParamRule<GameTeamColor>(
                GameTeamColor.Red,
                GameTeamColor.Red,
                GameTeamColor.Green,
                GameTeamColor.Blue,
                GameTeamColor.Yellow
            ),
        ];

    public override Task<bool> ExecuteAsync(IWiredExecutionContext ctx, CancellationToken ct)
    {
        int points = _wiredData.IntParams.Count > 0 ? _wiredData.GetIntParam<int>(0) : 0;
        int cap = _wiredData.IntParams.Count > 1 ? _wiredData.GetIntParam<int>(1) : 0;
        GameTeamColor team =
            _wiredData.IntParams.Count > 2
                ? _wiredData.GetIntParam<GameTeamColor>(2)
                : GameTeamColor.None;

        if (points != 0 && team != GameTeamColor.None)
        {
            _roomGrain.GameSystem.TryGiveScoreToTeam(_ctx.ObjectId, team, points, cap);
        }

        return Task.FromResult(true);
    }
}
