using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Context;
using Vortex.Furniture;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Snapshots.Wired.Variables;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Variables;

namespace Vortex.Rooms.Wired.Variables;

/// <summary>
/// Reads a shared wired variable out of a stored furni, without loading the room it stands in.
/// </summary>
/// <remarks>
/// A reference variable points at a variable in <em>another</em> room, and the dropdown offering
/// them has to be filled while that room sits in the database, unloaded. Activating every room a
/// player owns to ask each one what it shares is not an option, so this rebuilds the variable the
/// box would declare from the two things the furni row already carries: its id, which is the whole
/// of the variable id (<see cref="WiredVariableIdBuilder.CreateFromBoxId"/>), and its wired
/// configuration, which is JSON on the row.
/// <para>
/// That means the per-box knowledge below — target type, which int param holds the availability,
/// which flags follow from the rest — is stated twice: here, and in the logic class itself. It is
/// two boxes and it is covered by <c>WiredSharedVariableIndexTests</c>, which hydrates the real
/// logic and compares its own snapshot against this one, so a change to either side fails rather
/// than drifts.
/// </para>
/// </remarks>
public static class WiredSharedVariableIndex
{
    private static readonly string RoomVariableLogic = KeyOf(typeof(WiredVariableRoom));

    private static readonly string UserVariableLogic = KeyOf(typeof(WiredVariableUser));

    /// <summary>
    /// The logic keys whose boxes can be set to <see cref="WiredAvailabilityType.Shared"/> — the two
    /// whose <c>GetIntParamRules()</c> offer it. Read off the classes rather than spelled out, so
    /// renaming a box's client key cannot leave this list pointing at nothing.
    /// </summary>
    public static readonly string[] SharedCapableLogics = [RoomVariableLogic, UserVariableLogic];

    /// <summary>
    /// Rebuilds the variable a stored variable box declares, if that box is one that shares.
    /// </summary>
    /// <param name="logic">The furni definition's logic key.</param>
    /// <param name="boxId">The furni id, which is the box's room object id and so its variable id.</param>
    /// <param name="data">The box's persisted wired configuration.</param>
    public static bool TryDescribeShared(
        string logic,
        int boxId,
        WiredData data,
        out WiredVariableSnapshot snapshot
    )
    {
        snapshot = null!;

        // The availability lives in int param 0 on both boxes. Read raw rather than through
        // GetIntParam, which needs the rules the logic attaches during hydration: an out-of-range
        // value repairs to a default that is never Shared, so an exact match is the same answer.
        if (data.IntParams.Count == 0 || data.IntParams[0] != (int)WiredAvailabilityType.Shared)
        {
            return false;
        }

        WiredVariableTargetType targetType;
        WiredVariableFlags flags;

        if (logic == RoomVariableLogic)
        {
            targetType = WiredVariableTargetType.Global;
            flags =
                WiredVariableFlags.HasValue
                | WiredVariableFlags.CanWriteValue
                | WiredVariableFlags.CanInterceptChanges
                | WiredVariableFlags.AlwaysAvailable
                | WiredVariableFlags.CanReadLastUpdateTime;
        }
        else if (logic == UserVariableLogic)
        {
            targetType = WiredVariableTargetType.User;
            flags =
                (
                    data.IntParams.Count > 1 && data.IntParams[1] != 0
                        ? WiredVariableFlags.HasValue | WiredVariableFlags.CanWriteValue
                        : WiredVariableFlags.None
                )
                | WiredVariableFlags.CanCreateAndDelete
                | WiredVariableFlags.CanInterceptChanges
                | WiredVariableFlags.CanReadCreationTime;
        }
        else
        {
            return false;
        }

        // Neither box overrides GetTextConnectors(), so both hash and serialize an empty set.
        Dictionary<WiredVariableValue, string> textConnectors = [];

        snapshot = new WiredVariableSnapshot
        {
            VariableId = WiredVariableIdBuilder.CreateFromBoxId(boxId),
            VariableName = data.StringParam,
            VariableType = WiredVariableType.Created,
            VariableHash = WiredVariableHashBuilder.HashValues(
                data.StringParam,
                WiredAvailabilityType.Shared,
                targetType,
                flags,
                textConnectors
            ),
            AvailabilityType = WiredAvailabilityType.Shared,
            TargetType = targetType,
            Flags = flags,
            TextConnectors = textConnectors,
        };

        return true;
    }

    /// <summary>
    /// Every variable the rooms of <paramref name="ownerId"/> share, for the reference-variable
    /// dropdown.
    /// </summary>
    /// <remarks>
    /// Scoped to the rooms that player owns, and to nothing else: the rows are joined on the room's
    /// owner, so a room the player merely has rights in never contributes its owner's variable names
    /// to someone else's dialog.
    /// <para>
    /// Read from the database rather than from the rooms, which are mostly not running. The cost of
    /// that is freshness: a box whose configuration was changed moments ago in a room that is still
    /// loaded is marked dirty and written by that room's next save pass, so this can miss the last
    /// edit by that interval. A dropdown of other rooms' variables is the one place where that is
    /// the right trade.
    /// </para>
    /// </remarks>
    public static async Task<List<WiredVariableSharedSnapshot>> LoadForOwnerAsync(
        VortexDbContext dbCtx,
        int ownerId,
        CancellationToken ct
    )
    {
        var rows = await dbCtx
            .Furnitures.AsNoTracking()
            .Where(f =>
                f.RoomEntityId != null
                && f.DeletedAt == null
                && f.RoomEntity!.PlayerEntityId == ownerId
                && SharedCapableLogics.Contains(f.FurnitureDefinitionEntity!.Logic)
            )
            .Select(f => new
            {
                f.Id,
                f.ExtraData,
                Logic = f.FurnitureDefinitionEntity!.Logic,
                RoomId = f.RoomEntityId!.Value,
                RoomName = f.RoomEntity!.Name,
            })
            .ToListAsync(ct);

        List<WiredVariableSharedSnapshot> shared = [];

        foreach (var row in rows)
        {
            if (
                !new ExtraData(row.ExtraData).TryGetSection(
                    ExtraDataSectionType.WIRED,
                    out JsonElement wiredElement
                )
            )
            {
                continue;
            }

            WiredData? data = wiredElement.Deserialize<WiredData>();

            if (
                data is null
                || !TryDescribeShared(row.Logic, row.Id, data, out WiredVariableSnapshot variable)
            )
            {
                continue;
            }

            shared.Add(
                new WiredVariableSharedSnapshot
                {
                    Variable = variable,
                    RoomId = row.RoomId,
                    RoomName = row.RoomName,
                }
            );
        }

        return
        [
            .. shared
                .OrderBy(v => v.RoomName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(v => v.Variable.VariableName, StringComparer.OrdinalIgnoreCase),
        ];
    }

    private static string KeyOf(Type logic) =>
        logic.GetCustomAttributes<RoomObjectLogicAttribute>().Select(a => a.Key).First();
}
