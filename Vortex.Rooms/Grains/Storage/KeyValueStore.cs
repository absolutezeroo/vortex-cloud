using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Primitives.Rooms.Wired.Variable;

namespace Vortex.Rooms.Grains.Storage;

public sealed class KeyValueStore : IWiredVariableStore, IWiredKeyValueStore
{
    public Dictionary<string, WiredVariableValue> Store { get; set; } = [];

    /// <summary>
    /// When each key was first written and last written, in Unix milliseconds, for the "variable
    /// age" condition.
    /// </summary>
    /// <remarks>
    /// Wall clock, not the room clock: the store is persisted into the furni's extra data and the
    /// room clock restarts with the room, so a room-clock stamp would make every value look newly
    /// created after a reload. Values written before the room kept times simply have no entry, and
    /// the condition treats that as "unknown" rather than as "just now".
    /// </remarks>
    public Dictionary<string, long> CreatedAtMs { get; set; } = [];

    public Dictionary<string, long> UpdatedAtMs { get; set; } = [];

    private Func<Task>? _onChanged;

    public void SetAction(Func<Task>? onChanged) => _onChanged = onChanged;

    public bool ContainsKey(WiredVariableKey key) => Store.ContainsKey(key.ToStorageKey());

    public bool TryGetValue(in WiredVariableKey key, out WiredVariableValue value) =>
        Store.TryGetValue(key.ToStorageKey(), out value);

    public bool TryGetTimestamps(
        in WiredVariableKey key,
        out long createdAtMs,
        out long updatedAtMs
    )
    {
        string storageKey = key.ToStorageKey();

        createdAtMs = 0;
        updatedAtMs = 0;

        if (!Store.ContainsKey(storageKey))
        {
            return false;
        }

        bool hasCreated = CreatedAtMs.TryGetValue(storageKey, out createdAtMs);
        bool hasUpdated = UpdatedAtMs.TryGetValue(storageKey, out updatedAtMs);

        return hasCreated || hasUpdated;
    }

    public Task<bool> GiveValueAsync(
        WiredVariableKey key,
        WiredVariableValue value,
        bool replace = false
    )
    {
        if (Store.ContainsKey(key.ToStorageKey()) && !replace)
        {
            return Task.FromResult(false);
        }

        Store[key.ToStorageKey()] = value;
        Stamp(key.ToStorageKey());
        MarkDirty();

        return Task.FromResult(true);
    }

    public Task<bool> SetValueAsync(
        IWiredExecutionContext ctx,
        WiredVariableKey key,
        WiredVariableValue value
    )
    {
        if (!Store.ContainsKey(key.ToStorageKey()))
        {
            return Task.FromResult(false);
        }

        Store[key.ToStorageKey()] = value;
        Stamp(key.ToStorageKey());
        MarkDirty();

        return Task.FromResult(true);
    }

    public bool RemoveValue(WiredVariableKey key)
    {
        if (!Store.ContainsKey(key.ToStorageKey()) || !Store.Remove(key.ToStorageKey()))
        {
            return false;
        }

        CreatedAtMs.Remove(key.ToStorageKey());
        UpdatedAtMs.Remove(key.ToStorageKey());
        MarkDirty();

        return true;
    }

    /// <summary>Records the write. The creation time is only set the first time, so an "age since
    /// created" survives every later write to the same key.</summary>
    private void Stamp(string storageKey)
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        if (!CreatedAtMs.ContainsKey(storageKey))
        {
            CreatedAtMs[storageKey] = now;
        }

        UpdatedAtMs[storageKey] = now;
    }

    private void MarkDirty()
    {
        _ = _onChanged?.Invoke();
    }
}
