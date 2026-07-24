using System.Collections.Generic;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums.Games;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Wired.Rules;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Conditions;

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
            // None is the client's default option ("Any team" / "Triggerer's team"); rejecting it
            // makes the box impossible to save straight out of the catalogue.
            new WiredEnumParamRule<GameTeamColor>(
                GameTeamColor.None,
                GameTeamColor.None,
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

        GameTeamColor team = _wiredData.GetIntParam<GameTeamColor>(0);

        // None is the client's "Triggerer's team": resolve it against whoever set the trigger off. If
        // they are not on a team there is no score to compare, so the condition simply does not pass.
        if (team == GameTeamColor.None)
        {
            PlayerId triggerer = ctx.Event.CausedBy.PlayerId;

            team = triggerer > 0 ? _roomGrain.GameSystem.GetTeam(triggerer) : GameTeamColor.None;

            if (team == GameTeamColor.None)
            {
                return IsNegative();
            }
        }

        int score = _roomGrain.GameSystem.GetTeamScore(team);
        int threshold = _wiredData.GetIntParam<int>(1);

        bool result = _wiredData.GetIntParam<int>(2) switch
        {
            0 => score < threshold, // Lower than
            1 => score == threshold, // Equals
            2 => score > threshold, // Higher than
            _ => false,
        };

        return IsNegative() ? !result : result;
    }
}
