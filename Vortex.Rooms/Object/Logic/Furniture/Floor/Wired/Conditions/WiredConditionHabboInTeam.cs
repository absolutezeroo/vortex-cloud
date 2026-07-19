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

/// <summary>Passes when the triggering player is on the configured team (Habbo's "actor is on team").
/// Int param [0] is the team (1-4, matching <see cref="GameTeamColor"/>, from the client's team
/// selector). Reads the single source of truth in <see cref="Grains.Systems.RoomGameSystem"/>. The
/// negative variant inherits this and flips <see cref="FurnitureWiredConditionLogic.IsNegative"/>.</summary>
[RoomObjectLogic("wf_cnd_actor_in_team")]
public class WiredConditionHabboInTeam(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredConditionLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredConditionType.ACTOR_IS_IN_TEAM;

    public override List<IWiredParamRule> GetIntParamRules() =>
        [
            new WiredEnumParamRule<GameTeamColor>(
                GameTeamColor.Red,
                GameTeamColor.Red,
                GameTeamColor.Green,
                GameTeamColor.Blue,
                GameTeamColor.Yellow
            ),
        ];

    public override bool Evaluate(IWiredProcessingContext ctx)
    {
        PlayerId triggerer = ctx.Event.CausedBy.PlayerId;
        bool result = false;

        if (_wiredData.IntParams.Count > 0 && triggerer > 0)
        {
            result =
                _roomGrain.GameSystem.GetTeam(triggerer)
                == _wiredData.GetIntParam<GameTeamColor>(0);
        }

        return IsNegative() ? !result : result;
    }
}
