using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Primitives.Rooms.Wired.Variable;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Conditions;

/// <summary>
/// What the variable-reading conditions share: turning the box's configured variable (an id plus a
/// target type) into an actual value.
/// </summary>
/// <remarks>
/// A variable is stored per target — one value per furni, per user, or a single room-wide one — so
/// reading it needs the target ids the box is pointed at, which is the resolved input selection.
/// That resolution is asynchronous, so it happens in <see cref="PrepareAsync"/> and the evaluation
/// reads what it produced. A condition that never prepared therefore sees no targets and does not
/// pass, which is the same answer it gives for a variable that does not exist.
/// </remarks>
public abstract class FurnitureWiredVariableConditionLogic(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredConditionLogic(grainFactory, stuffDataFactory, ctx)
{
    private IWiredSelectionSet? _resolvedTargets;

    /// <summary>The variable's own target type, always int param [0] on these boxes: the source the
    /// client's picker was filtered by.</summary>
    protected WiredVariableTargetType VariableTarget =>
        _wiredData.IntParams.Count > 0
            ? _wiredData.GetIntParam<WiredVariableTargetType>(0)
            : WiredVariableTargetType.None;

    public override async Task PrepareAsync(IWiredProcessingContext ctx, CancellationToken ct) =>
        _resolvedTargets = await ctx.GetEffectiveSelectionAsync(this, ct);

    /// <summary>The configured variable id at this slot, or empty when the box was never pointed at
    /// one.</summary>
    protected string VariableIdAt(int index) =>
        _wiredData.VariableIds.Count > index ? _wiredData.VariableIds[index] : string.Empty;

    /// <summary>
    /// The first value this variable holds across the targets the box resolves to, if any.
    /// </summary>
    /// <remarks>
    /// "First" and not "all": none of these boxes carries the all/any radio that the furni-scoped
    /// conditions have, so the client offers no way to ask for more than existence.
    /// <para>
    /// Note the value is only meaningful when this returns true —
    /// <see cref="IWiredVariableStore.TryGetValue"/> pre-fills its out parameter with
    /// <see cref="WiredVariableValue.Default"/> (which is 1, not 0) before reporting a miss.
    /// </para>
    /// </remarks>
    protected bool TryReadVariable(
        string variableId,
        WiredVariableTargetType target,
        out WiredVariableValue value
    )
    {
        value = default;

        if (string.IsNullOrEmpty(variableId))
        {
            return false;
        }

        WiredVariableId id;

        try
        {
            id = WiredVariableId.Parse(variableId);
        }
        catch (Exception ex) when (ex is FormatException or OverflowException)
        {
            return false;
        }

        IWiredVariable? variable = _ctx.Furni.GetVariableById(id);

        if (variable is null)
        {
            return false;
        }

        foreach (int targetId in ResolveTargetIds(target))
        {
            if (variable.TryGetValue(new WiredVariableKey(id, target, targetId), out value))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Which ids the variable is keyed by, for this target type. Room-wide variables
    /// (global and context) are single-valued and key on 0, so they need no selection at all — they
    /// still answer in a stack whose selection is empty.</summary>
    private IEnumerable<int> ResolveTargetIds(WiredVariableTargetType target) =>
        target switch
        {
            WiredVariableTargetType.Furni => _resolvedTargets?.SelectedFurniIds ?? [],
            WiredVariableTargetType.User => _resolvedTargets?.SelectedPlayerIds ?? [],
            WiredVariableTargetType.Global or WiredVariableTargetType.Context => [0],
            _ => [],
        };
}
