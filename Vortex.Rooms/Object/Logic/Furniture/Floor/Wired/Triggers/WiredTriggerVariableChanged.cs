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
using Vortex.Primitives.Rooms.Snapshots.Wired.Variables;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Rooms.Wired;
using Vortex.Rooms.Wired.Rules;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Triggers;

/// <summary>
/// "The variable changed": fires when the variable it watches is created, written or deleted. This
/// closes the loop the variable boxes were missing — a room could write a variable and read one
/// back, but nothing could react to one moving.
/// </summary>
/// <remarks>
/// Int params are the three checkboxes plus the nested group under the middle one:
/// <c>[created, valueChanged, deleted, subMask]</c>, the mask carrying Increased, Decreased and
/// Unchanged as bits 0-2. The watched variable is variable id [0]; the box has no source-type param
/// because the target comes from the variable itself.
/// <para>
/// The client's picker filters on <c>canInterceptChanges</c>, so only variables that can report
/// their own writes are offered.
/// </para>
/// </remarks>
[RoomObjectLogic("wf_trg_var_changed")]
public class WiredTriggerVariableChanged(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredTriggerLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredTriggerType.VARIABLE_UPDATE;

    public override List<Type> SupportedEventTypes { get; } = [typeof(WiredVariableChangedEvent)];

    public override List<IWiredParamRule> GetIntParamRules() =>
        [
            new WiredBoolParamRule(false), // created
            new WiredBoolParamRule(true), // value changed
            new WiredBoolParamRule(false), // deleted
            new WiredRangeParamRule(0, 7, 0), // increased / decreased / unchanged, as bits 0-2
        ];

    public override int GetMaxVariableIds() => 1;

    public override List<WiredVariableContextSnapshot> GetWiredContextSnapshots() =>
        [
            new WiredVariableAllInRoomSnapshot()
            {
                ContextType = WiredContextType.AllVariablesInRoom,
                AllVariablesHash = _ctx.Furni.AllVariablesHash,
            },
        ];

    public override Task<bool> CanTriggerAsync(IWiredProcessingContext ctx, CancellationToken ct)
    {
        if (
            ctx.Event is not WiredVariableChangedEvent evt
            || _wiredData.IntParams.Count < 4
            || !WatchesVariable(evt.Key.VariableId)
        )
        {
            return Task.FromResult(false);
        }

        if (
            !WiredVariableChangeMatcher.Matches(
                evt.Kind,
                evt.Previous,
                evt.Current,
                _wiredData.GetIntParam<bool>(0),
                _wiredData.GetIntParam<bool>(1),
                _wiredData.GetIntParam<bool>(2),
                _wiredData.GetIntParam<int>(3)
            )
        )
        {
            return Task.FromResult(false);
        }

        // Whoever the value belonged to is the triggered target, so a stack can go on to act on
        // "the user whose score just changed" without a selector.
        switch (evt.Key.TargetType)
        {
            case WiredVariableTargetType.User:
                ctx.Selected.SelectedPlayerIds.Add(evt.Key.TargetId);

                break;
            case WiredVariableTargetType.Furni:
                ctx.Selected.SelectedFurniIds.Add(evt.Key.TargetId);

                break;
        }

        return Task.FromResult(true);
    }

    private bool WatchesVariable(WiredVariableId changed)
    {
        if (_wiredData.VariableIds.Count == 0)
        {
            return false;
        }

        try
        {
            return WiredVariableId.Parse(_wiredData.VariableIds[0]) == changed;
        }
        catch (Exception ex) when (ex is FormatException or OverflowException)
        {
            return false;
        }
    }
}
