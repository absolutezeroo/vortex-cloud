using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Snapshots.Wired.Variables;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Rooms.Grains.Storage;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Variables;
using Vortex.Rooms.Wired;
using Vortex.Rooms.Wired.Variables;

namespace Vortex.Rooms.Grains.Systems;

public sealed partial class RoomWiredSystem
{
    private readonly HashSet<int> _dirtyVariableBoxIds = [];

    private readonly FurnitureActiveStore _furnitureActiveStore = new();
    private readonly PlayerActiveStore _playerActiveStore = new();
    private readonly RoomActiveStore _roomActiveStore = new();
    private readonly Dictionary<WiredVariableId, IWiredVariable> _variableById = [];

    /// <summary>
    /// Every variable a box put in the room, so removing the box removes all of them.
    /// </summary>
    /// <remarks>
    /// A list rather than one id: an add-on stacked with a variable box adds variables derived from
    /// it — level and progress from an experience value, year and hour from a timestamp, a Variable
    /// FX display from anything. They belong to the box, not to the add-on, because the box is what
    /// the room tracks and what a pickup takes away.
    /// </remarks>
    private readonly Dictionary<int, List<WiredVariableId>> _variableIdBoxId = [];

    private WiredVariablesSnapshot? _variablesSnapshot;

    public IWiredVariable? GetVariableById(WiredVariableId id)
    {
        if (_variableById.TryGetValue(id, out IWiredVariable? variable))
        {
            return variable;
        }

        return null;
    }

    public bool TryGetStoreForKey(WiredVariableKey key, out KeyValueStore? store)
    {
        return key.TargetType switch
        {
            WiredVariableTargetType.Furni => _furnitureActiveStore.TryGetStore(key, out store),
            WiredVariableTargetType.User => _playerActiveStore.TryGetStore(key, out store),
            WiredVariableTargetType.Global => _roomActiveStore.TryGetStore(key, out store),
            _ => throw new ArgumentOutOfRangeException(
                nameof(key.TargetType),
                $"Unsupported target type: {key.TargetType}"
            ),
        };
    }

    public Task<WiredVariablesSnapshot> GetWiredVariablesSnapshotAsync(CancellationToken ct)
    {
        return Task.FromResult(_variablesSnapshot ??= BuildVariablesSnapshot());
    }

    public Task<
        List<(WiredVariableId id, WiredVariableValue value)>
    > GetAllVariablesForBindingAsync(WiredVariableBinding binding, CancellationToken ct)
    {
        List<(WiredVariableId id, WiredVariableValue value)> variableValues = new();

        foreach ((WiredVariableId id, IWiredVariable variable) in _variableById)
        {
            WiredVariableKey key = new(id, binding.TargetType, binding.TargetId);

            if (!variable.TryGetValue(key, out WiredVariableValue value))
            {
                continue;
            }

            variableValues.Add((id, value));
        }

        return Task.FromResult(variableValues);
    }

    /// <summary>Finds a room-scoped runtime variable by its display name and enumerates the
    /// current holders (players/furni in this room with a stored value), for the wired-menu
    /// "highlight holders" panel. Distinct from the persisted permanent-variable store — this
    /// reads live in-memory bindings only.</summary>
    public Task<(
        WiredVariableSnapshot Variable,
        List<(int ObjectId, int Value)> Holders
    )?> GetVariableHoldersByNameAsync(string variableName, CancellationToken ct)
    {
        IWiredVariable? variable = _variableById.Values.FirstOrDefault(v =>
            v.GetVarSnapshot().VariableName == variableName
        );

        if (variable is null)
        {
            return Task.FromResult<(WiredVariableSnapshot, List<(int, int)>)?>(null);
        }

        WiredVariableSnapshot snapshot = variable.GetVarSnapshot();
        List<(int ObjectId, int Value)> holders = new();

        IEnumerable<int> candidateIds = snapshot.TargetType switch
        {
            WiredVariableTargetType.User => Room.AllAvatarPlayerIds(),
            WiredVariableTargetType.Furni => Room.AllItemIds(),
            _ => [0],
        };

        foreach (int targetId in candidateIds)
        {
            WiredVariableKey key = new(snapshot.VariableId, snapshot.TargetType, targetId);

            if (variable.TryGetValue(key, out WiredVariableValue value))
            {
                holders.Add((targetId, value));
            }
        }

        return Task.FromResult<(WiredVariableSnapshot, List<(int, int)>)?>((snapshot, holders));
    }

    private Task ProcessInternalVariablesAsync(long now, CancellationToken ct)
    {
        IEnumerable<IWiredVariable> variables = _host.InternalVariables();

        foreach (IWiredVariable variable in variables)
        {
            ProcessVariable(variable);
        }

        return Task.CompletedTask;
    }

    private async Task ProcessVariableBoxesAsync(long now, CancellationToken ct)
    {
        if (_dirtyVariableBoxIds.Count == 0)
        {
            return;
        }

        List<int> dirtyVariableBoxIds = _dirtyVariableBoxIds.ToList();
        _dirtyVariableBoxIds.Clear();

        foreach (int boxId in dirtyVariableBoxIds)
        {
            await ProcessVariableBoxAsync(boxId, ct);
        }

        _variablesSnapshot = null;
    }

    private async Task ProcessVariableBoxAsync(int boxId, CancellationToken ct)
    {
        RemoveVariableBox(boxId);

        if (
            !Room.TryGetItem(boxId, out IRoomItem? item)
            || item.Logic is not FurnitureWiredVariableLogic variable
        )
        {
            return;
        }

        await variable.LoadWiredAsync(ct);

        if (!ProcessVariable(variable))
        {
            return;
        }

        List<WiredVariableId> registered = [variable.GetVarSnapshot().VariableId];

        foreach (IWiredVariable derived in await BuildSubVariablesAsync(item, variable, ct))
        {
            if (ProcessVariable(derived))
            {
                registered.Add(derived.GetVarSnapshot().VariableId);
            }
        }

        _variableIdBoxId[boxId] = registered;
    }

    /// <summary>
    /// The variables the add-ons stacked with this box derive from it.
    /// </summary>
    /// <remarks>
    /// The pile is resolved from the box's own tile, which is also how a firing stack finds its
    /// add-ons — the resolver leaves variable boxes out of a pile deliberately, but the add-ons
    /// standing with them are exactly what this needs.
    /// <para>
    /// Each contributing add-on is hydrated first. An add-on the room has not loaded yet answers
    /// from a blank configuration, which produces a set of sub-variables named after nothing, and
    /// they would sit in the registry until the next time the box happened to go dirty.
    /// </para>
    /// </remarks>
    private async Task<List<IWiredVariable>> BuildSubVariablesAsync(
        IRoomItem box,
        FurnitureWiredVariableLogic parent,
        CancellationToken ct
    )
    {
        List<IWiredVariable> derived = [];

        if (box is not IRoomFloorItem floor)
        {
            return derived;
        }

        WiredStack stack = await _stacks.BuildFromTileAsync(Room.ToIdx(floor.X, floor.Y), ct);

        foreach (IWiredAddon addon in stack.Addons)
        {
            if (addon is not IWiredSubVariableSource source)
            {
                continue;
            }

            try
            {
                await addon.LoadWiredAsync(ct);

                derived.AddRange(source.CreateSubVariables(parent));
            }
            catch (Exception ex)
            {
                Diagnostics.Logger.LogWarning(
                    ex,
                    "Wired add-on {AddonType} failed to derive sub-variables in room {RoomId}.",
                    addon.GetType().Name,
                    Room.RoomId
                );
            }
        }

        return derived;
    }

    private bool ProcessVariable(IWiredVariable variable)
    {
        WiredVariableSnapshot snapshot = variable.GetVarSnapshot();

        if (string.IsNullOrWhiteSpace(snapshot.VariableName))
        {
            return false;
        }

        _variableById[snapshot.VariableId] = variable;

        return true;
    }

    private void RemoveVariableBox(int boxId)
    {
        if (!_variableIdBoxId.TryGetValue(boxId, out List<WiredVariableId>? variableIds))
        {
            return;
        }

        _variableIdBoxId.Remove(boxId);

        // Every id the box registered, not just its own: a box whose add-on derived four
        // sub-variables would otherwise leave four entries pointing at a logic the room has
        // dropped, and the next reader of the variable list would resolve them.
        foreach (WiredVariableId variableId in variableIds)
        {
            _variableById.Remove(variableId);
        }
    }

    private WiredVariablesSnapshot BuildVariablesSnapshot()
    {
        List<WiredVariableHash> hashes = new();
        List<WiredVariableSnapshot> snapshots = new(_variableById.Count);

        foreach (IWiredVariable variable in _variableById.Values)
        {
            WiredVariableSnapshot snapshot = variable.GetVarSnapshot();

            hashes.Add(snapshot.VariableHash);
            snapshots.Add(snapshot);
        }

        WiredVariablesSnapshot allVariablesSnapshot = new()
        {
            AllVariablesHash = WiredVariableHashBuilder.HashFromHashes(hashes),
            Variables = snapshots,
        };

        Room.AllVariablesHash = allVariablesSnapshot.AllVariablesHash;

        return allVariablesSnapshot;
    }
}
