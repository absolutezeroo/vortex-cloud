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
/// A reading that has an inverse can also be written, and the write lands on the parent: asking for
/// level 5 is asking for the experience at which level 5 begins. The level is indeed an opinion
/// about the XP — but one you can run backwards, which is the part this used to get wrong by
/// refusing every write outright. A reading with no inverse still refuses, and now says so in its
/// flags instead of only in its behaviour.
/// </para>
/// <para>
/// <see cref="GiveValueAsync"/> and <see cref="RemoveValue"/> refuse in every case. Those belong to
/// the "give variable" and "remove variable" boxes, which filter on
/// <see cref="WiredVariableFlags.CanCreateAndDelete"/>, and a derived reading is neither created nor
/// deleted on its own — it exists exactly as long as the add-on says it does.
/// </para>
/// </remarks>
internal sealed class WiredLevelUpSubVariable(
    IWiredVariable parent,
    WiredVariableId variableId,
    string variableName,
    WiredLevelUpCurve curve,
    Func<WiredLevelUpCurve, int, int> read,
    Func<WiredLevelUpCurve, int, int, int>? write
) : IWiredVariable, IWiredDerivedVariable
{
    private WiredVariableSnapshot? _snapshot;

    /// <summary>The variable box this reading was derived from. The room asks so it can turn that
    /// box's change into one for this reading — the only announcement this reading will ever
    /// get, since every write lands on the parent.</summary>
    public WiredVariableId SourceVariableId => ParentSnapshot.VariableId;

    public int ValueFor(int sourceValue) => read(curve, sourceValue);

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

        value = new WiredVariableValue(ValueFor(raw.Value));

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

    /// <summary>
    /// Runs the reading backwards and writes the result to the parent.
    /// </summary>
    /// <remarks>
    /// The current experience is read first because most inverses need it: "set the progress to 20"
    /// means twenty into <em>this</em> level, and which level that is only the parent can say. The
    /// parent then does the writing, so the change event, the persistence and the value clamp are
    /// the ones the variable box already has — this adds arithmetic and nothing else.
    /// </remarks>
    public async Task<bool> SetValueAsync(
        IWiredExecutionContext ctx,
        WiredVariableKey key,
        WiredVariableValue value
    )
    {
        if (write is null || !CanBind(key))
        {
            return false;
        }

        WiredVariableKey parentKey = key with { VariableId = ParentSnapshot.VariableId };

        if (!parent.TryGetValue(parentKey, out WiredVariableValue current))
        {
            return false;
        }

        return await parent.SetValueAsync(
            ctx,
            parentKey,
            new WiredVariableValue(write(curve, current.Value, value.Value))
        );
    }

    public bool RemoveValue(WiredVariableKey key) => false;

    public WiredVariableSnapshot GetVarSnapshot() => _snapshot ??= BuildSnapshot();

    private WiredVariableSnapshot ParentSnapshot => parent.GetVarSnapshot();

    /// <summary>
    /// What the four wired dialogs are allowed to offer this reading for.
    /// </summary>
    /// <remarks>
    /// Every variable picker in the wired editor filters on one flag and greys out — rather than
    /// hides — whatever fails it, so a capability that exists in code and is missing from this set is
    /// a row the builder can see and cannot click. Each flag here answers one of them:
    /// <list type="bullet">
    /// <item><see cref="WiredVariableFlags.CanInterceptChanges"/> — the "variable changed" trigger
    /// (<c>VariableUpdate</c>), which is the one chain the add-on exists for: fire when the level
    /// goes up.</item>
    /// <item><see cref="WiredVariableFlags.CanWriteValue"/> — the "change variable value" action
    /// (<c>ChangeVariable</c>), and only for the readings that have an inverse. The three that do
    /// not stay greyed there, correctly this time.</item>
    /// <item><see cref="WiredVariableFlags.CanReadCreationTime"/> and
    /// <see cref="WiredVariableFlags.CanReadLastUpdateTime"/> — the "variable age" condition, taken
    /// from the parent rather than asserted, because <see cref="TryGetTimestamps"/> is the parent's
    /// answer verbatim. If the parent keeps no times, neither does its readings.</item>
    /// </list>
    /// <para>
    /// Never <see cref="WiredVariableFlags.CanCreateAndDelete"/>, which the "give" and "remove"
    /// boxes filter on: a reading is brought into being by the add-on and by nothing else.
    /// </para>
    /// </remarks>
    private WiredVariableSnapshot BuildSnapshot()
    {
        WiredVariableSnapshot parentSnapshot = ParentSnapshot;
        Dictionary<WiredVariableValue, string> textConnectors = [];

        WiredVariableFlags flags =
            WiredVariableFlags.HasValue
            | WiredVariableFlags.CanInterceptChanges
            | (
                parentSnapshot.Flags
                & (
                    WiredVariableFlags.CanReadCreationTime
                    | WiredVariableFlags.CanReadLastUpdateTime
                )
            );

        if (write is not null)
        {
            flags |= WiredVariableFlags.CanWriteValue;
        }

        return new()
        {
            VariableId = variableId,
            VariableName = variableName,
            VariableType = WiredVariableType.Created,
            VariableHash = WiredVariableHashBuilder.HashValues(
                variableName,
                parentSnapshot.AvailabilityType,
                parentSnapshot.TargetType,
                flags,
                textConnectors
            ),
            AvailabilityType = parentSnapshot.AvailabilityType,
            TargetType = parentSnapshot.TargetType,
            Flags = flags,
            TextConnectors = textConnectors,
        };
    }
}
