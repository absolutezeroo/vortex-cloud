using System.Collections.Generic;
using Orleans;
using Turbo.Primitives.Furniture.Providers;
using Turbo.Primitives.Rooms.Enums.Games;
using Turbo.Primitives.Rooms.Enums.Wired;
using Turbo.Primitives.Rooms.Object.Furniture.Floor;
using Turbo.Primitives.Rooms.Object.Logic;
using Turbo.Primitives.Rooms.Wired;
using Turbo.Rooms.Wired.Rules;

namespace Turbo.Rooms.Object.Logic.Furniture.Floor.Wired.Conditions;

/// <summary>Passes when a team's score compares as configured against a threshold (Habbo's "team has
/// points"). Int params from the client setup form (conditions/TeamHasScore): [0] = team (1-4, matching
/// <see cref="GameTeamColor"/>), [1] = threshold (0-1000), [2] = comparison operator (0 = equal,
/// 1 = less than, 2 = greater than — the same comparison radio the altitude condition uses). Reads the
/// single source of truth in <see cref="Grains.Systems.RoomGameSystem"/>. No negative variant.</summary>
[RoomObjectLogic("wf_cnd_team_has_score")]
public class WiredConditionTeamHasScore(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredConditionLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredConditionType.TEAM_HAS_SCORE;

    public override List<IWiredParamRule> GetIntParamRules() =>
        [
            new WiredEnumParamRule<GameTeamColor>(
                GameTeamColor.Red,
                GameTeamColor.Red,
                GameTeamColor.Green,
                GameTeamColor.Blue,
                GameTeamColor.Yellow
            ),
            new WiredRangeParamRule(0, 1000, 0), // threshold
            new WiredRangeParamRule(0, 2, 0), // comparison operator
        ];

    public override bool Evaluate(IWiredProcessingContext ctx)
    {
        if (_wiredData.IntParams.Count < 3)
        {
            return IsNegative();
        }

        int score = _roomGrain.GameSystem.GetTeamScore(_wiredData.GetIntParam<GameTeamColor>(0));
        int threshold = _wiredData.GetIntParam<int>(1);

        bool result = _wiredData.GetIntParam<int>(2) switch
        {
            0 => score == threshold,
            1 => score < threshold,
            2 => score > threshold,
            _ => false,
        };

        return IsNegative() ? !result : result;
    }
}
