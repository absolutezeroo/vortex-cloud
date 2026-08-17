using System;
using System.Collections.Generic;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Object.Logic;

namespace Vortex.Rooms.Grains;

/// <summary>
/// Incrementally maintained index of the room's items by the concrete type of their attached logic,
/// including every base class, so <c>ItemsOf&lt;SomeFamilyBaseLogic&gt;()</c> finds the whole family.
/// It replaces the "walk all of <c>ItemsById</c> and pattern-match the logic" scans that every game
/// system used to carry — those ran on the hot path (the game-timer one on every 50 ms frame).
/// <para>
/// Invariant: an item is in the index if and only if it is in <see cref="RoomLiveState.ItemsById"/>
/// with its logic attached. The three places an item leaves the live state (pickup/removal in
/// <c>RoomObjectModule.RemoveObjectAsync</c>, the definition swap in <c>RoomGrain.Furni.Edit.cs</c>,
/// pet food eaten in <c>RoomPetSystem.Motion</c>) and the one place logic attaches
/// (<c>RoomObjectModule.AttatchLogicAsync</c>) all notify it — a new mutation path for
/// <c>ItemsById</c> MUST do the same or the index silently goes stale.
/// </para>
/// All access happens inside the room grain's single-threaded turn, so no locking.
/// </summary>
public sealed class RoomItemIndex
{
    private readonly Dictionary<Type, HashSet<IRoomItem>> _itemsByLogicType = [];

    /// <summary>Registers the item under its logic's concrete type and every base class (exclusive
    /// of <see cref="object"/>), so family-level queries see derived logics.</summary>
    public void OnLogicAttached(IRoomItem item)
    {
        if (item.Logic is null)
        {
            return;
        }

        for (
            Type? type = item.Logic.GetType();
            type is not null && type != typeof(object);
            type = type.BaseType
        )
        {
            if (!_itemsByLogicType.TryGetValue(type, out HashSet<IRoomItem>? bucket))
            {
                bucket = [];
                _itemsByLogicType[type] = bucket;
            }

            bucket.Add(item);
        }
    }

    /// <summary>Drops the item from every bucket it was registered under. A no-op for an item whose
    /// logic never attached (it was never indexed).</summary>
    public void OnItemDetached(IRoomItem item)
    {
        if (item.Logic is null)
        {
            return;
        }

        for (
            Type? type = item.Logic.GetType();
            type is not null && type != typeof(object);
            type = type.BaseType
        )
        {
            if (_itemsByLogicType.TryGetValue(type, out HashSet<IRoomItem>? bucket))
            {
                bucket.Remove(item);

                if (bucket.Count == 0)
                {
                    _itemsByLogicType.Remove(type);
                }
            }
        }
    }

    /// <summary>The live bucket for <typeparamref name="TLogic"/> — do not enumerate this across an
    /// <c>await</c> that can add or remove items; use <see cref="LogicsOf{TLogic}"/> for that.</summary>
    public IReadOnlyCollection<IRoomItem> ItemsOf<TLogic>()
        where TLogic : class, IRoomObjectLogic =>
        _itemsByLogicType.TryGetValue(typeof(TLogic), out HashSet<IRoomItem>? bucket) ? bucket : [];

    /// <summary>A materialised snapshot of every <typeparamref name="TLogic"/> in the room — safe to
    /// iterate while awaiting state changes (which can detach items mid-loop).</summary>
    public List<TLogic> LogicsOf<TLogic>()
        where TLogic : class, IRoomObjectLogic
    {
        List<TLogic> logics = [];

        if (_itemsByLogicType.TryGetValue(typeof(TLogic), out HashSet<IRoomItem>? bucket))
        {
            foreach (IRoomItem item in bucket)
            {
                if (item.Logic is TLogic logic)
                {
                    logics.Add(logic);
                }
            }
        }

        return logics;
    }
}
