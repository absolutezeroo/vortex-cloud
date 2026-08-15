using System.Collections.Generic;

namespace Vortex.Rooms.Wired;

/// <summary>
/// A small time-to-live cache the wired boxes fill asynchronously and then read synchronously.
/// <para>
/// It exists because <see cref="Vortex.Primitives.Rooms.Wired.IWiredCondition.Evaluate"/> is
/// synchronous by contract (it runs inside the room's own turn and may not await), while the data
/// some conditions need — a guild roster, a player's worn badges — lives behind a grain. The box
/// warms this cache from its asynchronous prepare step and the evaluation then reads the warmed
/// entry, so a stack that fires every tick costs one lookup rather than one query.
/// </para>
/// <para>
/// The clock is passed in on every call rather than read here: the room clock is the only clock the
/// wired engine is allowed to observe, and keeping it a parameter makes the expiry testable.
/// </para>
/// </summary>
public sealed class WiredTtlCache<TKey, TValue>(long ttlMs)
    where TKey : notnull
{
    private readonly Dictionary<TKey, Entry> _entries = [];

    private readonly long _ttlMs = ttlMs;

    /// <summary>Whether this key holds an entry that has not expired yet, i.e. whether a refresh can
    /// be skipped.</summary>
    public bool IsFresh(TKey key, long nowMs) =>
        _entries.TryGetValue(key, out Entry entry) && entry.ExpiresAtMs > nowMs;

    /// <summary>The cached value, when one is present and still fresh. An expired entry is reported
    /// as a miss and left in place; the next <see cref="Set"/> overwrites it.</summary>
    public bool TryGet(TKey key, long nowMs, out TValue? value)
    {
        if (_entries.TryGetValue(key, out Entry entry) && entry.ExpiresAtMs > nowMs)
        {
            value = entry.Value;

            return true;
        }

        value = default;

        return false;
    }

    public void Set(TKey key, TValue value, long nowMs) =>
        _entries[key] = new Entry(value, nowMs + _ttlMs);

    /// <summary>Drops everything, for when the underlying data is known to have moved (a guild
    /// roster refresh, say) rather than merely aged.</summary>
    public void Clear() => _entries.Clear();

    private readonly record struct Entry(TValue Value, long ExpiresAtMs);
}
