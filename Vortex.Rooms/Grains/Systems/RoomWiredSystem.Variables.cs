using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Snapshots.Wired.Variables;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Rooms.Grains.Storage;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Variables;
using Vortex.Rooms.Wired.Variables;

namespace Vortex.Rooms.Grains.Systems;

public sealed partial class RoomWiredSystem
{
    private readonly HashSet<int> _dirtyVariableBoxIds = [];

    private readonly FurnitureActiveStore _furnitureActiveStore = new();
    private readonly PlayerActiveStore _playerActiveStore = new();
    private readonly RoomActiveStore _roomActiveStore = new();
    private readonly Dictionary<WiredVariableId, IWiredVariable> _variableById = [];
    private readonly Dictionary<int, WiredVariableId> _variableIdBoxId = [];

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
            WiredVariableTargetType.User => _roomGrain._state.AvatarsByPlayerId.Keys.Select(p =>
                p.Value
            ),
            WiredVariableTargetType.Furni => _roomGrain._state.ItemsById.Keys.Select(i => i.Value),
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
        IEnumerable<IWiredVariable> variables =
            _roomGrain._wiredVariablesProvider.BuildVariablesForRoom(_roomGrain);

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
            !_roomGrain._state.ItemsById.TryGetValue(boxId, out IRoomItem? item)
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

        WiredVariableSnapshot snapshot = variable.GetVarSnapshot();

        _variableIdBoxId[boxId] = snapshot.VariableId;
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
        if (!_variableIdBoxId.TryGetValue(boxId, out WiredVariableId variableId))
        {
            return;
        }

        _variableIdBoxId.Remove(boxId);
        _variableById.Remove(variableId);
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

        _roomGrain._state.AllVariablesHash = allVariablesSnapshot.AllVariablesHash;

        return allVariablesSnapshot;
    }
}
