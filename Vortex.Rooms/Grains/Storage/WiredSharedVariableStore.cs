using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Vortex.Logging.Extensions;
using Vortex.Primitives.Rooms.Grains;
using Vortex.Primitives.Rooms.Snapshots.Wired.Variables;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Primitives.Rooms.Wired.Variable;

namespace Vortex.Rooms.Grains.Storage;

/// <summary>
/// A room's read replica of a shared wired variable, over the grain that owns it.
/// </summary>
/// <remarks>
/// <see cref="IWiredKeyValueStore.TryGetValue"/> is synchronous by contract — it is read from inside
/// a condition, in the room's own turn, where nothing may await — while the values live behind a
/// grain. So reads are served from a local copy and writes go to the grain first and only reach the
/// copy once it has accepted them: reporting a write the owner refused would let two rooms disagree
/// permanently, which is the one thing a shared variable may not do.
/// <para>
/// The copy is refreshed when the box hydrates, so a room sees another room's writes from its next
/// refresh rather than instantly. That lag is the price of a synchronous read, and it is bounded by
/// <see cref="RefreshIntervalMs"/>.
/// </para>
/// </remarks>
internal sealed class WiredSharedVariableStore(
    IWiredSharedVariableGrain grain,
    ILogger logger,
    Func<long>? nowMs = null
) : IWiredKeyValueStore
{
    /// <summary>How stale a replica may get before the next hydration refetches it.</summary>
    /// <remarks>
    /// ponytail: a fixed interval rather than the grain telling its readers when something moved.
    /// Push would be live and is a lot of machinery for a box most rooms do not own; raise this to a
    /// subscription if shared variables ever drive something that has to react at once.
    /// </remarks>
    private const long RefreshIntervalMs = 5_000;

    private readonly IWiredSharedVariableGrain _grain = grain;
    private readonly ILogger _logger = logger;

    // Wall clock, not the room clock: this paces a network call, and it has to keep pacing it the
    // same way for two rooms whose clocks started at different moments.
    private readonly Func<long> _nowMs =
        nowMs ?? (() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

    private readonly Dictionary<string, WiredSharedValueSnapshot> _replica = [];

    /// <summary>When the copy was last fetched, and null while it never has been. Nullable rather
    /// than a sentinel: <c>now - long.MinValue</c> overflows to a negative number, which reads as
    /// "refreshed a moment ago" and skips the one refresh that is never optional — the first.
    /// </summary>
    private long? _refreshedAtMs;

    /// <summary>Refetches the values unless the copy is still young enough.</summary>
    public async Task RefreshAsync(CancellationToken ct, bool force = false)
    {
        long now = _nowMs();

        if (!force && _refreshedAtMs is long last && now - last < RefreshIntervalMs)
        {
            return;
        }

        try
        {
            ImmutableArray<WiredSharedValueSnapshot> values = await _grain.GetAllAsync(ct);

            _replica.Clear();

            foreach (WiredSharedValueSnapshot value in values)
            {
                _replica[value.StorageKey] = value;
            }

            _refreshedAtMs = now;
        }
        catch (Exception ex)
        {
            // The copy is left as it was. A shared variable that cannot be reached reads as whatever
            // it last was, which is a better answer than suddenly holding nothing.
            _logger.LogWarning(ex, "Failed to refresh a shared wired variable replica.");
        }
    }

    public bool ContainsKey(WiredVariableKey key) => _replica.ContainsKey(key.ToStorageKey());

    public bool TryGetValue(in WiredVariableKey key, out WiredVariableValue value)
    {
        if (_replica.TryGetValue(key.ToStorageKey(), out WiredSharedValueSnapshot? found))
        {
            value = new WiredVariableValue(found.Value);

            return true;
        }

        value = WiredVariableValue.Default;

        return false;
    }

    public bool TryGetTimestamps(
        in WiredVariableKey key,
        out long createdAtMs,
        out long updatedAtMs
    )
    {
        createdAtMs = 0;
        updatedAtMs = 0;

        if (!_replica.TryGetValue(key.ToStorageKey(), out WiredSharedValueSnapshot? found))
        {
            return false;
        }

        createdAtMs = found.CreatedAtMs;
        updatedAtMs = found.UpdatedAtMs;

        return true;
    }

    public async Task<bool> GiveValueAsync(
        WiredVariableKey key,
        WiredVariableValue value,
        bool replace = false
    )
    {
        string storageKey = key.ToStorageKey();

        if (!await _grain.GiveAsync(storageKey, value, replace, CancellationToken.None))
        {
            return false;
        }

        Record(storageKey, value);

        return true;
    }

    public async Task<bool> SetValueAsync(
        IWiredExecutionContext ctx,
        WiredVariableKey key,
        WiredVariableValue value
    )
    {
        string storageKey = key.ToStorageKey();

        if (!await _grain.SetAsync(storageKey, value, CancellationToken.None))
        {
            return false;
        }

        Record(storageKey, value);

        return true;
    }

    /// <summary>
    /// Removal is synchronous by contract, so the grain is told without being waited for.
    /// </summary>
    /// <remarks>
    /// The local answer is the one the caller gets, and it is the honest one for this room: the key
    /// is gone from here. A grain that then refuses the delete brings the value back at the next
    /// refresh, which is the same repair every other disagreement between the two gets.
    /// </remarks>
    public bool RemoveValue(WiredVariableKey key)
    {
        string storageKey = key.ToStorageKey();

        if (!_replica.Remove(storageKey))
        {
            return false;
        }

        _grain
            .RemoveAsync(storageKey, CancellationToken.None)
            .LogAndForget(_logger, "Failed to remove a shared wired variable value.");

        return true;
    }

    private void Record(string storageKey, int value)
    {
        long now = _nowMs();
        long createdAtMs = _replica.TryGetValue(storageKey, out WiredSharedValueSnapshot? existing)
            ? existing.CreatedAtMs
            : now;

        _replica[storageKey] = new WiredSharedValueSnapshot
        {
            StorageKey = storageKey,
            Value = value,
            CreatedAtMs = createdAtMs,
            UpdatedAtMs = now,
        };
    }
}
