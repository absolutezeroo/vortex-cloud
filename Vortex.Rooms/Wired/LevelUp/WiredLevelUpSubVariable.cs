using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Snapshots.Wired.Variables;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Rooms.Wired.Variables;

namespace Vortex.Rooms.Wired.LevelUp;

/// <summary>
/// One of the eight readings a level-up add-on takes of the variable it is stacked with — the
/// level, the progress, the XP still owed, and so on.
/// </summary>
/// <remarks>
/// It owns no value. Every read goes to the parent variable under the caller's own key, with only
/// the variable id swapped, so a per-user experience variable yields a per-user level and a global
/// one a global level, with nothing here deciding which.
/// <para>
/// Read-only, and silently so: <see cref="GiveValueAsync"/>, <see cref="SetValueAsync"/> and
/// <see cref="RemoveValue"/> all refuse. Writing "level" is meaningless — the level is an opinion
/// about the XP, and the way to change it is to change the XP. A wired box that tries gets false,
/// which is the same answer it gets for any variable it may not write.
/// </para>
/// </remarks>
internal sealed class WiredLevelUpSubVariable(
    IWiredVariable parent,
    WiredVariableId variableId,
    string variableName,
    WiredLevelUpCurve curve,
    Func<WiredLevelUpCurve, int, int> read
) : IWiredVariable
{
    private WiredVariableSnapshot? _snapshot;

    public bool CanBind(in WiredVariableKey key) =>
        key.VariableId == variableId && key.TargetType == ParentSnapshot.TargetType;

    public bool TryGetValue(in WiredVariableKey key, out WiredVariableValue value)
    {
        WiredVariableKey parentKey = key with { VariableId = ParentSnapshot.VariableId };

        if (!parent.TryGetValue(parentKey, out WiredVariableValue raw))
        {
            value = WiredVariableValue.Default;

            return false;
        }

        value = new WiredVariableValue(read(curve, raw.Value));

        return true;
    }

    /// <summary>The parent's, verbatim: a derived reading is exactly as old as what it reads.</summary>
    public bool TryGetTimestamps(
        in WiredVariableKey key,
        out long createdAtMs,
        out long updatedAtMs
    )
    {
        WiredVariableKey parentKey = key with { VariableId = ParentSnapshot.VariableId };

        return parent.TryGetTimestamps(parentKey, out createdAtMs, out updatedAtMs);
    }

    public Task<bool> GiveValueAsync(
        WiredVariableKey key,
        WiredVariableValue value,
        bool replace = false
    ) => Task.FromResult(false);

    public Task<bool> SetValueAsync(
        IWiredExecutionContext ctx,
        WiredVariableKey key,
        WiredVariableValue value
    ) => Task.FromResult(false);

    public bool RemoveValue(WiredVariableKey key) => false;

    public WiredVariableSnapshot GetVarSnapshot() => _snapshot ??= BuildSnapshot();

    private WiredVariableSnapshot ParentSnapshot => parent.GetVarSnapshot();

    private WiredVariableSnapshot BuildSnapshot()
    {
        WiredVariableSnapshot parentSnapshot = ParentSnapshot;
        Dictionary<WiredVariableValue, string> textConnectors = [];

        return new()
        {
            VariableId = variableId,
            VariableName = variableName,
            VariableType = WiredVariableType.Created,
            VariableHash = WiredVariableHashBuilder.HashValues(
                variableName,
                parentSnapshot.AvailabilityType,
                parentSnapshot.TargetType,
                WiredVariableFlags.HasValue,
                textConnectors
            ),
            AvailabilityType = parentSnapshot.AvailabilityType,
            TargetType = parentSnapshot.TargetType,
            Flags = WiredVariableFlags.HasValue,
            TextConnectors = textConnectors,
        };
    }
}
