using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Events;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Wired;
using Vortex.Rooms.Wired.Rules;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Triggers;

[RoomObjectLogic("wf_trg_score_achieved")]
public class WiredTriggerScoreAchieved(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredTriggerLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredTriggerType.SCORE_ACHIEVED;
    public override List<Type> SupportedEventTypes { get; } = [typeof(WiredTeamScoreChangedEvent)];

    // Client ScoreAchieved.ts: intParams = [score (1-1000), team (0 = any, 1-4 = Red/Green/Blue/Yellow)].
    public override List<IWiredParamRule> GetIntParamRules() =>
        [new WiredRangeParamRule(1, 1000, 1), new WiredRangeParamRule(0, 4, 0)];

    public override Task<bool> CanTriggerAsync(IWiredProcessingContext ctx, CancellationToken ct)
    {
        if (ctx.Event is not WiredTeamScoreChangedEvent evt)
        {
            return Task.FromResult(false);
        }

        int score = _wiredData.IntParams.Count > 0 ? _wiredData.GetIntParam<int>(0) : 1;
        int team = _wiredData.IntParams.Count > 1 ? _wiredData.GetIntParam<int>(1) : 0;

        return Task.FromResult(
            WiredScoreAchievedMatcher.Matches(
                team,
                score,
                (int)evt.Team,
                evt.Score,
                evt.PreviousScore
            )
        );
    }
}
