using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Turbo.Primitives.Rooms.Enums.Wired;
using Turbo.Primitives.Rooms.Object.Furniture;
using Turbo.Primitives.Rooms.Snapshots.Wired.Variables;
using Turbo.Primitives.Rooms.Wired.Variable;
using Turbo.Rooms.Grains.Storage;
using Turbo.Rooms.Object.Logic.Furniture.Floor.Wired.Variables;
using Turbo.Rooms.Wired.Variables;

namespace Turbo.Rooms.Grains.Systems;

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
            )
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
        List<WiredVariableSnapshot> snapshots = new(
            _variableById.Count
        );

        foreach (IWiredVariable variable in _variableById.Values)
        {
            WiredVariableSnapshot snapshot = variable.GetVarSnapshot();

            hashes.Add(snapshot.VariableHash);
            snapshots.Add(snapshot);
        }

        WiredVariablesSnapshot allVariablesSnapshot = new()
        {
            AllVariablesHash = WiredVariableHashBuilder.HashFromHashes(hashes),
            Variables = snapshots
        };

        _roomGrain._state.AllVariablesHash = allVariablesSnapshot.AllVariablesHash;

        return allVariablesSnapshot;
    }
}
