using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Actions;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Addons;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Conditions;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Selectors;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Triggers;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Variables;

namespace Vortex.Rooms.Wired.Engine;

/// <summary>
/// Which trigger boxes are in the room, indexed by the event types they listen for.
/// </summary>
/// <remarks>
/// <para>
/// It is a registry of boxes, <b>not</b> a cache of resolved piles. A box's tile is read live at fire
/// time and the pile it drives is resolved live from that tile — so a box that has been moved or
/// picked up simply is not on the tile the resolver looks at, and there is no stale window to
/// invalidate. Daybreak needs an invalidatable stack index because it is multithreaded; under a
/// single grain turn, reading truth at fire time is both simpler and correct.
/// </para>
/// <para>
/// What does need rebuilding is membership: which boxes exist and what they listen for. That is
/// flagged by <see cref="MarkDirty"/> when wired furniture is added, removed, moved or reconfigured,
/// and once per tick at most.
/// </para>
/// </remarks>
internal sealed class WiredTriggerIndex(IWiredRoomView room, IWiredDiagnostics diagnostics)
{
    private readonly IWiredRoomView _room = room;
    private readonly IWiredDiagnostics _diagnostics = diagnostics;

    private readonly Dictionary<Type, List<FurnitureWiredTriggerLogic>> _byEventType = [];
    private readonly List<FurnitureWiredTriggerLogic> _timed = [];

    /// <summary>Starts dirty: an empty index has never been built, which is not the same as a room
    /// with no triggers in it.</summary>
    public bool IsDirty { get; private set; } = true;

    public void MarkDirty() => IsDirty = true;

    /// <summary>No trigger of any kind. Nothing queued can ever be consumed, so the caller drops it.</summary>
    public bool IsEmpty => _byEventType.Count == 0 && _timed.Count == 0;

    public bool Listens(Type eventType) => _byEventType.ContainsKey(eventType);

    /// <summary>
    /// The timed triggers, in a stable order. Safe to loop by index across awaits: the registry is
    /// only rebuilt at the top of a tick, never during a pass.
    /// </summary>
    public IReadOnlyList<FurnitureWiredTriggerLogic> Timed => _timed;

    /// <summary>
    /// The triggers listening for one event type, as a snapshot. A snapshot because firing an action
    /// can mutate room furniture — and a stale registry entry marks the index dirty — so iterating
    /// the live list would be iterating something the loop itself is changing.
    /// </summary>
    public IReadOnlyList<FurnitureWiredTriggerLogic> Listening(Type eventType) =>
        _byEventType.TryGetValue(eventType, out List<FurnitureWiredTriggerLogic>? triggers)
            ? [.. triggers]
            : [];

    /// <summary>
    /// Re-reads every wired trigger in the room and clears the dirty flag.
    /// </summary>
    /// <remarks>
    /// Each trigger is hydrated as it is indexed so a timed one has its schedule ready to be polled
    /// on this same tick. A trigger that will not hydrate is skipped with a warning rather than
    /// taking the rebuild — and therefore every other trigger in the room — down with it.
    /// </remarks>
    public async Task RebuildAsync(CancellationToken ct)
    {
        _byEventType.Clear();
        _timed.Clear();

        foreach (IRoomItem item in _room.AllItems())
        {
            if (item.Logic is not FurnitureWiredTriggerLogic trigger)
            {
                continue;
            }

            try
            {
                await trigger.LoadWiredAsync(ct);
            }
            catch (Exception ex)
            {
                _diagnostics.Logger.LogWarning(
                    ex,
                    "Failed to hydrate wired trigger {ItemId} in room {RoomId}.",
                    item.ObjectId,
                    _room.RoomId
                );

                continue;
            }

            if (trigger is IWiredTimedTrigger)
            {
                _timed.Add(trigger);
            }

            foreach (Type eventType in trigger.SupportedEventTypes)
            {
                if (
                    !_byEventType.TryGetValue(eventType, out List<FurnitureWiredTriggerLogic>? list)
                )
                {
                    list = [];
                    _byEventType[eventType] = list;
                }

                list.Add(trigger);
            }
        }

        IsDirty = false;
    }
}
