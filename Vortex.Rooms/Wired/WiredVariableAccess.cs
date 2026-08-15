using System;
using System.Collections.Generic;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Primitives.Rooms.Wired.Variable;

namespace Vortex.Rooms.Wired;

/// <summary>
/// Turning a box's configured variable — an id, a target type, and the selection the box resolves
/// to — into the live variable and the ids it is keyed by. Shared by the conditions that read a
/// variable and the action that changes one, so both agree on what a target is.
/// </summary>
public static class WiredVariableAccess
{
    /// <summary>The live variable behind a configured id, or false when the id is malformed or
    /// names a variable the room no longer has.</summary>
    public static bool TryResolve(
        IRoomFurniAccess furni,
        string variableId,
        out WiredVariableId id,
        out IWiredVariable? variable
    )
    {
        id = default;
        variable = null;

        if (string.IsNullOrEmpty(variableId))
        {
            return false;
        }

        try
        {
            id = WiredVariableId.Parse(variableId);
        }
        catch (Exception ex) when (ex is FormatException or OverflowException)
        {
            return false;
        }

        variable = furni.GetVariableById(id);

        return variable is not null;
    }

    /// <summary>
    /// Which ids the variable is keyed by for this target type. Room-wide variables (global and
    /// context) are single-valued and key on 0, so they answer even in a stack whose selection is
    /// empty.
    /// </summary>
    public static IEnumerable<int> TargetIds(
        WiredVariableTargetType target,
        IWiredSelectionSet? selection
    ) =>
        target switch
        {
            WiredVariableTargetType.Furni => selection?.SelectedFurniIds ?? [],
            WiredVariableTargetType.User => selection?.SelectedPlayerIds ?? [],
            WiredVariableTargetType.Global or WiredVariableTargetType.Context => [0],
            _ => [],
        };

    /// <summary>
    /// The first value this variable holds across the box's targets, if any.
    /// </summary>
    /// <remarks>
    /// The value is only meaningful when this returns true:
    /// <see cref="IWiredVariableStore.TryGetValue"/> pre-fills its out parameter with
    /// <see cref="WiredVariableValue.Default"/> — which is 1, not 0 — before reporting a miss, so a
    /// caller that ignores the result reads an unwritten variable as holding 1.
    /// </remarks>
    public static bool TryRead(
        IRoomFurniAccess furni,
        string variableId,
        WiredVariableTargetType target,
        IWiredSelectionSet? selection,
        out WiredVariableValue value
    )
    {
        value = default;

        if (!TryResolve(furni, variableId, out WiredVariableId id, out IWiredVariable? variable))
        {
            return false;
        }

        foreach (int targetId in TargetIds(target, selection))
        {
            if (variable!.TryGetValue(new WiredVariableKey(id, target, targetId), out value))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// When the first of the box's targets holding this variable was written, for the age
    /// condition. False when nothing holds it, or when it is a computed variable (which is derived
    /// on every read and so was never written) — an unknown age must not read as an age of zero.
    /// </summary>
    public static bool TryReadTimestamps(
        IRoomFurniAccess furni,
        string variableId,
        WiredVariableTargetType target,
        IWiredSelectionSet? selection,
        out long createdAtMs,
        out long updatedAtMs
    )
    {
        createdAtMs = 0;
        updatedAtMs = 0;

        if (!TryResolve(furni, variableId, out WiredVariableId id, out IWiredVariable? variable))
        {
            return false;
        }

        foreach (int targetId in TargetIds(target, selection))
        {
            if (
                variable!.TryGetTimestamps(
                    new WiredVariableKey(id, target, targetId),
                    out createdAtMs,
                    out updatedAtMs
                )
            )
            {
                return true;
            }
        }

        return false;
    }
}
