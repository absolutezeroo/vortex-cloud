using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Triggers;
using Vortex.Rooms.Wired.Rules;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Actions;

[RoomObjectLogic("wf_act_adjust_clock")]
public class WiredActionAdjustClock(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredActionLogic(grainFactory, stuffDataFactory, ctx)
{
    // Client AdjustClock.ts operator radio (loc operator.*): 0 Increase, 1 Decrease, 2 Set value.
    private const int OperatorIncrease = 0;
    private const int OperatorDecrease = 1;
    private const int OperatorSet = 2;

    public override int WiredCode => (int)WiredActionType.ADJUST_CLOCK;

    // Client AdjustClock.ts: intParams = [seconds (0-119), minutes (0-99), subPulse (0-1), operator (0-2)].
    public override List<IWiredParamRule> GetIntParamRules() =>
        [
            new WiredRangeParamRule(0, 119, 0),
            new WiredRangeParamRule(0, 99, 0),
            new WiredBoolParamRule(false),
            new WiredRangeParamRule(0, 2, OperatorSet),
        ];

    public override List<WiredFurniSourceType[]> GetAllowedFurniSources() =>
        [
            [WiredFurniSourceType.SelectedItems, WiredFurniSourceType.SelectorItems],
        ];

    public override async Task<bool> ExecuteAsync(IWiredExecutionContext ctx, CancellationToken ct)
    {
        int seconds = _wiredData.IntParams.Count > 0 ? _wiredData.GetIntParam<int>(0) : 0;
        int minutes = _wiredData.IntParams.Count > 1 ? _wiredData.GetIntParam<int>(1) : 0;
        int op = _wiredData.IntParams.Count > 3 ? _wiredData.GetIntParam<int>(3) : OperatorSet;

        int totalSeconds = (minutes * 60) + seconds;

        IWiredSelectionSet selection = await ctx.GetEffectiveSelectionAsync(this, ct);

        foreach (int furniId in selection.SelectedFurniIds)
        {
            if (
                !_roomGrain._state.ItemsById.TryGetValue(furniId, out IRoomItem? item)
                || item.Logic is not IWiredCounter counter
            )
            {
                continue;
            }

            switch (op)
            {
                case OperatorIncrease:
                    counter.AddClockSeconds(totalSeconds);
                    break;
                case OperatorDecrease:
                    counter.AddClockSeconds(-totalSeconds);
                    break;
                case OperatorSet:
                    counter.SetClockSeconds(totalSeconds);
                    break;
            }
        }

        return true;
    }
}
