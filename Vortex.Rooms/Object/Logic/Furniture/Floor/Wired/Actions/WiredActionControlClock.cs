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

[RoomObjectLogic("wf_act_control_clock")]
public class WiredActionControlClock(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredActionLogic(grainFactory, stuffDataFactory, ctx)
{
    // Client ControlClock.ts radio (loc clock_control.*): 0 Start, 1 Stop, 2 Reset, 3 Pause, 4 Resume.
    private const int ControlStart = 0;
    private const int ControlStop = 1;
    private const int ControlReset = 2;
    private const int ControlPause = 3;
    private const int ControlResume = 4;

    public override int WiredCode => (int)WiredActionType.CONTROL_CLOCK;

    public override List<IWiredParamRule> GetIntParamRules() => [new WiredRangeParamRule(0, 4, 0)];

    public override List<WiredFurniSourceType[]> GetAllowedFurniSources() =>
        [
            [WiredFurniSourceType.SelectedItems, WiredFurniSourceType.SelectorItems],
        ];

    public override async Task<bool> ExecuteAsync(IWiredExecutionContext ctx, CancellationToken ct)
    {
        int control =
            _wiredData.IntParams.Count > 0 ? _wiredData.GetIntParam<int>(0) : ControlStart;

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

            switch (control)
            {
                case ControlStart:
                    counter.StartClock();
                    break;
                case ControlStop:
                case ControlPause:
                    counter.HaltClock();
                    break;
                case ControlReset:
                    counter.ResetClock();
                    break;
                case ControlResume:
                    counter.ResumeClock();
                    break;
            }
        }

        return true;
    }
}
