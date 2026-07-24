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

/// <summary>
/// "The team is in a given position" (client TeamIsWinning.ts). Int params are
/// <c>[team, placement]</c>: the team is 0 for the triggerer's own team or 1-4 for a colour, and the
/// placement is 0-3 for the loc labels <c>placement.1</c>..<c>.4</c> — 1st through 4th.
/// <para>
/// Teams are ranked by score, highest first, with ties sharing the better position: a team is Nth when
/// exactly N-1 teams score strictly more than it. Two teams tied at the top are therefore both 1st,
/// which is what "is the team winning" is asking.
/// </para>
/// </summary>
[RoomObjectLogic("wf_cnd_team_has_rank")]
public class WiredConditionTeamHasRank(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredConditionLogic(grainFactory, stuffDataFactory, ctx)
{
    private static readonly GameTeamColor[] Teams =
    [
        GameTeamColor.Red,
        GameTeamColor.Green,
        GameTeamColor.Blue,
        GameTeamColor.Yellow,
    ];

    public override int WiredCode => (int)WiredConditionType.TEAM_IS_WINNING;

    public override List<IWiredParamRule> GetIntParamRules() =>
        [
            // None is the client's default "Triggerer's team".
            new WiredEnumParamRule<GameTeamColor>(
                GameTeamColor.None,
                GameTeamColor.None,
                GameTeamColor.Red,
                GameTeamColor.Green,
                GameTeamColor.Blue,
                GameTeamColor.Yellow
            ),
            new WiredRangeParamRule(0, 3, 0), // placement: 0 = 1st ... 3 = 4th
        ];

    public override bool Evaluate(IWiredProcessingContext ctx)
    {
        if (_wiredData.IntParams.Count < 2)
        {
            return IsNegative();
        }

        GameTeamColor team = _wiredData.GetIntParam<GameTeamColor>(0);

        if (team == GameTeamColor.None)
        {
            PlayerId triggerer = ctx.Event.CausedBy.PlayerId;

            team = triggerer > 0 ? _roomGrain.GameSystem.GetTeam(triggerer) : GameTeamColor.None;

            // Nobody, or somebody with no team: there is no placement to compare against.
            if (team == GameTeamColor.None)
            {
                return IsNegative();
            }
        }

        int requiredPlacement = _wiredData.GetIntParam<int>(1) + 1;
        int score = _roomGrain.GameSystem.GetTeamScore(team);
        int placement = 1;

        foreach (GameTeamColor other in Teams)
        {
            if (other != team && _roomGrain.GameSystem.GetTeamScore(other) > score)
            {
                placement++;
            }
        }

        bool result = placement == requiredPlacement;

        return IsNegative() ? !result : result;
    }
}
