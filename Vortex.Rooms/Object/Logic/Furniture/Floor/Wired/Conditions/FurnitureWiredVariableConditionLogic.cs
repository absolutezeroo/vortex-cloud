using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Rooms.Wired;

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

    /// <summary>The first value the variable holds across the targets this box resolves to. Only
    /// meaningful when it returns true — see <see cref="WiredVariableAccess.TryRead"/>.</summary>
    protected bool TryReadVariable(
        string variableId,
        WiredVariableTargetType target,
        out WiredVariableValue value
    ) => WiredVariableAccess.TryRead(_ctx.Furni, variableId, target, _resolvedTargets, out value);

    /// <summary>When the variable was written, for the age condition. Only meaningful when it
    /// returns true — see <see cref="WiredVariableAccess.TryReadTimestamps"/>.</summary>
    protected bool TryReadTimestamps(
        string variableId,
        WiredVariableTargetType target,
        out long createdAtMs,
        out long updatedAtMs
    ) =>
        WiredVariableAccess.TryReadTimestamps(
            _ctx.Furni,
            variableId,
            target,
            _resolvedTargets,
            out createdAtMs,
            out updatedAtMs
        );
}
