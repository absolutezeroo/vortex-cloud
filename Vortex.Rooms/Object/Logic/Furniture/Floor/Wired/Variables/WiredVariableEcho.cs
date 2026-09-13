using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Snapshots.Wired.Variables;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Primitives.Rooms.Wired.Variable;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Variables;

/// <summary>
/// A second name for a variable that already exists: every read of this box answers with the value
/// of the variable it points at.
/// </summary>
/// <remarks>
/// The client's form (variables, code 7) has exactly two fields — a name and a variable picker — and
/// its <c>variableType()</c> returns the picked variable's own target rather than a fixed one. So
/// this box does not own a scope: it takes the scope of what it mirrors, and a user variable echoed
/// here is still per-user, read against the same user.
/// <para>
/// The name is the only thing the box adds, and that is the point: the picker hands the room a flat
/// default name for the source, which is what makes a furni variable addressable from a placeholder
/// or from a box that only knows names.
/// </para>
/// <para>
/// <b>A pass-through, not a copy.</b> Reads, writes, timestamps and text connectors all go to the
/// mirrored variable; the box stores nothing of its own. That is the documented purpose — the
/// variable add-ons used to stack only on user-created variables, and the echo is what lets them
/// stack on an internal, smart or sub-variable, "so that all variables can benefit from variable
/// add-ons", with the original editable through the give/remove/modify effects when it supports
/// modification at all. It therefore publishes the source's flags unmasked: a box that read
/// correctly and swallowed writes would be the same bug as no box at all, one dialog further in.
/// </para>
/// </remarks>
[RoomObjectLogic("wf_var_echo")]
public class WiredVariableEcho(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredVariableLogic(grainFactory, stuffDataFactory, ctx)
{
    /// <summary>What an echo offers when it mirrors nothing: a value slot and nothing else, so an
    /// unconfigured box neither disappears from the menu nor advertises an operation it cannot
    /// perform.</summary>
    private const WiredVariableFlags UnresolvedFlags =
        WiredVariableFlags.HasValue | WiredVariableFlags.AlwaysAvailable;

    /// <summary>Guards an echo chain that loops back on itself. Two boxes pointing at each other is
    /// a configuration a player can build by hand, and the resolution below would otherwise follow
    /// it until the stack ran out.</summary>
    private bool _resolving;

    public override int WiredCode => (int)WiredVariableBoxType.Echo;

    public override int GetMaxVariableIds() => 1;

    protected override WiredVariableTargetType TargetType =>
        FromSource(source => source.GetVarSnapshot().TargetType, WiredVariableTargetType.None);

    protected override WiredAvailabilityType AvailabilityType => WiredAvailabilityType.Reference;

    /// <summary>
    /// The mirrored variable's own flags, write flags included.
    /// </summary>
    /// <remarks>
    /// The point of the box is that the add-ons which only ever stacked on user-created variables
    /// can now stack on an internal or smart one, and that includes changing it: "if the original
    /// internal variable supports modification, it can be edited through the give, remove and modify
    /// variable effects". Masking the write flags off would publish a box that reads correctly and
    /// silently swallows every write aimed at it.
    /// </remarks>
    protected override WiredVariableFlags Flags =>
        FromSource(source => source.GetVarSnapshot().Flags, UnresolvedFlags);

    public override bool TryGetValue(in WiredVariableKey key, out WiredVariableValue value)
    {
        value = WiredVariableValue.Default;

        if (!CanBind(key) || !TryEnter(out IWiredVariable? source, out WiredVariableId sourceId))
        {
            return false;
        }

        try
        {
            // Same target, different name: an echo of a user variable is still read against the
            // user the stack resolved, not against the box.
            return source!.TryGetValue(Rebind(key, sourceId), out value);
        }
        finally
        {
            _resolving = false;
        }
    }

    public override bool TryGetTimestamps(
        in WiredVariableKey key,
        out long createdAtMs,
        out long updatedAtMs
    )
    {
        createdAtMs = 0;
        updatedAtMs = 0;

        if (!CanBind(key) || !TryEnter(out IWiredVariable? source, out WiredVariableId sourceId))
        {
            return false;
        }

        try
        {
            return source!.TryGetTimestamps(
                Rebind(key, sourceId),
                out createdAtMs,
                out updatedAtMs
            );
        }
        finally
        {
            _resolving = false;
        }
    }

    public override Dictionary<WiredVariableValue, string> GetTextConnectors() =>
        FromSource(source => source.GetVarSnapshot().TextConnectors, []);

    /// <summary>
    /// Writes reach the mirrored variable, which is where the value actually lives.
    /// </summary>
    /// <remarks>
    /// Nothing is stored on the echo itself. Whether the write is allowed at all is the source's
    /// answer to give — its own <c>CanCreateAndDelete</c> check refuses a variable that may not be
    /// created, and the change event it publishes is the one a "variable changed" trigger is already
    /// listening for. An echo that kept its own copy would let the two drift, and the whole point of
    /// the box is that there is one value under two names.
    /// </remarks>
    public override async Task<bool> GiveValueAsync(
        WiredVariableKey key,
        WiredVariableValue value,
        bool replace = false
    )
    {
        if (!CanBind(key) || !TryEnter(out IWiredVariable? source, out WiredVariableId sourceId))
        {
            return false;
        }

        try
        {
            return await source!.GiveValueAsync(Rebind(key, sourceId), value, replace);
        }
        finally
        {
            _resolving = false;
        }
    }

    /// <inheritdoc cref="GiveValueAsync"/>
    public override async Task<bool> SetValueAsync(
        IWiredExecutionContext ctx,
        WiredVariableKey key,
        WiredVariableValue value
    )
    {
        if (!CanBind(key) || !TryEnter(out IWiredVariable? source, out WiredVariableId sourceId))
        {
            return false;
        }

        try
        {
            return await source!.SetValueAsync(ctx, Rebind(key, sourceId), value);
        }
        finally
        {
            _resolving = false;
        }
    }

    /// <inheritdoc cref="GiveValueAsync"/>
    public override bool RemoveValue(WiredVariableKey key)
    {
        if (!CanBind(key) || !TryEnter(out IWiredVariable? source, out WiredVariableId sourceId))
        {
            return false;
        }

        try
        {
            return source!.RemoveValue(Rebind(key, sourceId));
        }
        finally
        {
            _resolving = false;
        }
    }

    /// <summary>The same target under the mirrored variable's name: an echo of a user variable is
    /// still read and written against the user the stack resolved, not against the box.</summary>
    private static WiredVariableKey Rebind(in WiredVariableKey key, WiredVariableId sourceId) =>
        new(sourceId, key.TargetType, key.TargetId);

    /// <summary>
    /// Asks the mirrored variable something, holding the recursion guard for as long as the answer
    /// is being read.
    /// </summary>
    /// <remarks>
    /// The guard has to span the <em>use</em> of the source and not merely its lookup, which is the
    /// whole reason this is a method taking a function rather than a <c>Source</c> property.
    /// <see cref="TargetType"/>, <see cref="Flags"/> and <see cref="GetTextConnectors"/> all run
    /// inside <see cref="FurnitureWiredVariableLogic.BuildVarSnapshot"/> and all ask the source for
    /// <em>its</em> snapshot — so with the guard released at the end of the lookup, two echoes
    /// pointing at each other build each other's snapshot until the stack runs out. A property
    /// looked correct and overflowed on the first test that built the cycle.
    /// <para>
    /// Two echoes pointing at each other is a configuration a player can assemble by hand, and the
    /// symptom would not be a broken box: it would be the room's activation dying. Inside such a
    /// cycle the box reports no target and read-only flags, which is the truthful answer — an echo
    /// of an echo of itself names no variable.
    /// </para>
    /// </remarks>
    private T FromSource<T>(Func<IWiredVariable, T> read, T whenUnresolved)
    {
        if (_resolving)
        {
            return whenUnresolved;
        }

        _resolving = true;

        try
        {
            return TryResolveSource(out IWiredVariable? source, out _) && source is not null
                ? read(source)
                : whenUnresolved;
        }
        finally
        {
            _resolving = false;
        }
    }

    /// <summary>Resolves the source and claims the recursion guard. The caller releases it.</summary>
    private bool TryEnter(out IWiredVariable? source, out WiredVariableId sourceId)
    {
        source = null;
        sourceId = default;

        if (_resolving || !TryResolveSource(out source, out sourceId))
        {
            return false;
        }

        _resolving = true;

        return true;
    }

    private bool TryResolveSource(out IWiredVariable? source, out WiredVariableId sourceId)
    {
        source = null;
        sourceId = default;

        if (
            !GetValidVariableIds(_wiredData.VariableIds, out List<WiredVariableId> ids)
            || ids.Count == 0
        )
        {
            return false;
        }

        sourceId = ids[0];
        source = _ctx.Furni.GetVariableById(sourceId);

        // An echo of itself resolves to itself, and every read of it would be a read of the value
        // it is trying to produce.
        if (source is null || ReferenceEquals(source, this))
        {
            source = null;

            return false;
        }

        return true;
    }
}
