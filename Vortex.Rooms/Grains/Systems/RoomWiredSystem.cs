using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Vortex.Primitives.Action;
using Vortex.Primitives.Messages.Outgoing.Room.Engine;
using Vortex.Primitives.Rooms;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Events;
using Vortex.Primitives.Rooms.Events.Player;
using Vortex.Primitives.Rooms.Events.RoomItem;
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
using Vortex.Rooms.Wired;
using Vortex.Rooms.Wired.Logs;

namespace Vortex.Rooms.Grains.Systems;

public sealed partial class RoomWiredSystem(RoomGrain roomGrain) : IRoomEventListener
{
    private readonly Queue<RoomEvent> _eventQueue = new();

    private readonly Dictionary<
        WiredExecutionKey,
        WiredPendingStackExecution
    > _pendingStackExecutions = [];

    private readonly RoomGrain _roomGrain = roomGrain;

    // Trigger box registries — NOT caches of resolved stacks. They only hold references to the trigger
    // boxes present in the room; a box's tile is read live at fire time and the pile it drives is
    // resolved live from that tile (BuildStackFromTileAsync). Because the RoomGrain is single-threaded
    // (no tick/room race), reading truth at fire time has no stale window — so there is no per-tile
    // dirty flag, no cached WiredStack, and no co-location guard: a moved/removed box simply is not on
    // the tile we resolve. The registries are rebuilt (RebuildTriggerIndexAsync) only when wired
    // furniture is added/removed/moved/reconfigured, signalled by _indexDirty.
    private readonly Dictionary<Type, List<FurnitureWiredTriggerLogic>> _triggersByEventType = [];
    private readonly List<FurnitureWiredTriggerLogic> _timedTriggers = [];
    private bool _indexDirty = true;

    private readonly PriorityQueue<(WiredExecutionKey key, long version), long> _stackSchedule =
        new();

    private bool _firstRun = true;
    private long _nextStackExecutionId;

    // Boxes currently lit by FlashActivationStateAsync, mapped to the room-clock time their visual
    // state reverts to unlit. Re-flashing a box simply pushes its revert time back.
    private readonly Dictionary<RoomObjectId, long> _flashRevertAtMs = [];

    // Events rejected because the queue hit WiredMaxQueuedEvents, reported once per tick in the
    // room's wired log instead of spamming one entry per drop.
    private int _droppedEventCount;

    // The room-clock time of the tick currently being processed, so an executing action (e.g. the Timer
    // Reset effect) can re-anchor schedules to "now" without threading the value through every call.
    private long _currentTickMs;

    private int _tickMs => _roomGrain._roomConfig.WiredTickMs;

    public Task OnRoomEventAsync(RoomEvent evt, CancellationToken ct)
    {
        if (evt is null)
        {
            return Task.CompletedTask;
        }

        switch (evt)
        {
            case RoomWiredStackChangedEvent:
                // A wired box was attached, detached, moved or reconfigured. Membership of the trigger
                // registries may have changed, so flag them for rebuild on the next tick. Piles are
                // resolved live at fire time, so nothing else needs invalidating here.
                _indexDirty = true;
                break;
            case WiredVariableBoxChangedEvent boxEvt:
                {
                    foreach (int boxId in boxEvt.BoxIds)
                    {
                        _dirtyVariableBoxIds.Add(boxId);
                    }
                }
                break;
            case PlayerLeftEvent playerLeftEvt:
                _playerActiveStore.RemovePlayerStore(playerLeftEvt.PlayerId);
                EnqueueRoomEvent(evt);
                break;
            case RoomItemDetachedEvent detatchedEvt:
                _furnitureActiveStore.RemoveFurnitureStore(detatchedEvt.ObjectId);
                break;
            default:
                EnqueueRoomEvent(evt);
                break;
        }

        return Task.CompletedTask;
    }

    private void EnqueueRoomEvent(RoomEvent evt)
    {
        // With a clean index we know exactly which event types have a listening trigger; anything
        // else would only consume dequeue budget before being discarded, so reject it now. A dirty
        // index means membership is unknown until the next tick's rebuild — enqueue conservatively.
        if (!_indexDirty && !_triggersByEventType.ContainsKey(evt.GetType()))
        {
            return;
        }

        // WiredMaxEventsPerTick bounds the tick's work; this bounds the queue's memory under a
        // sustained storm. Rejecting the incoming event (rather than evicting an older one) keeps
        // trigger ordering intact for what was already accepted.
        if (_eventQueue.Count >= _roomGrain._roomConfig.WiredMaxQueuedEvents)
        {
            _droppedEventCount++;

            return;
        }

        _eventQueue.Enqueue(evt);
    }

    public async Task ProcessWiredAsync(long now, CancellationToken ct)
    {
        if (now < _roomGrain._state.NextWiredBoundaryMs)
        {
            return;
        }

        while (now >= _roomGrain._state.NextWiredBoundaryMs)
        {
            _roomGrain._state.NextWiredBoundaryMs += _tickMs;
        }

        _currentTickMs = now;

        await ProcessFlashRevertsAsync(now, ct);

        if (_droppedEventCount > 0)
        {
            WriteWiredRoomLog(
                WiredLogLevel.Warning,
                WiredLogSource.System,
                $"Dropped {_droppedEventCount} room event(s): the wired event queue was full."
            );

            _droppedEventCount = 0;
        }

        if (_firstRun)
        {
            await ProcessInternalVariablesAsync(now, ct);

            _firstRun = false;
        }

        await ProcessVariableBoxesAsync(now, ct);

        if (_indexDirty)
        {
            await RebuildTriggerIndexAsync(ct);

            _indexDirty = false;
        }

        // Run action chains scheduled on earlier ticks that are now due (delayed effects resuming).
        await RunDueScheduledStackExecutionsAsync(now, ct);

        if (_triggersByEventType.Count == 0 && _timedTriggers.Count == 0)
        {
            // No wired triggers in the room: nothing can consume queued room events, so drop them
            // rather than let the queue grow unbounded.
            _eventQueue.Clear();

            return;
        }

        await ProcessTimedTriggersAsync(now, ct);

        int budget = _roomGrain._roomConfig.WiredMaxEventsPerTick;

        while (budget-- > 0 && _eventQueue.Count > 0)
        {
            RoomEvent evt = _eventQueue.Dequeue();

            await ProcessRoomEventAsync(evt, now, ct);
        }

        // Run the zero-delay chains just scheduled by this tick's fires, so trigger -> effect happens
        // within the same tick instead of one tick (~50ms) later.
        await RunDueScheduledStackExecutionsAsync(now, ct);
    }

    private async Task ProcessTimedTriggersAsync(long now, CancellationToken ct)
    {
        // Index-based: the registry is stable for the duration of this pass (it is only rebuilt at the
        // top of the tick), so an index loop is safe across the awaits below.
        for (int i = 0; i < _timedTriggers.Count; i++)
        {
            FurnitureWiredTriggerLogic trigger = _timedTriggers[i];

            if (trigger is not IWiredTimedTrigger timed)
            {
                continue;
            }

            // A box lingering in the registry after being picked up: skip it and reindex next tick.
            if (!_roomGrain._state.ItemsById.ContainsKey(trigger.ObjectId))
            {
                _indexDirty = true;

                continue;
            }

            if (!timed.TryConsumeDue(now))
            {
                continue;
            }

            // Resolve the pile the trigger currently sits on, live. If it was dragged onto an empty
            // tile the pile has no actions and nothing fires — the "same pile" rule, for free.
            WiredStack stack = await BuildStackFromTileAsync(trigger.TileIdx, ct);

            await FireTriggerWithEventAsync(
                trigger,
                new PeriodicRoomEvent
                {
                    RoomId = _roomGrain.RoomId,
                    CausedBy = ActionContext.CreateForWired(_roomGrain.RoomId),
                },
                stack,
                now,
                ct
            );
        }
    }

    /// <summary>
    /// Server side of the Timer Reset effect (<c>wf_act_reset_timers</c>): restart every resettable timed
    /// trigger in the room — repeaters re-anchor (fire next tick, interval afresh) and "at given time"
    /// one-shots re-arm so they can fire again. Room-wide, matching Habbo (the effect takes the room, not
    /// the pile).
    /// </summary>
    public void ResetTimers()
    {
        foreach (FurnitureWiredTriggerLogic trigger in _timedTriggers)
        {
            if (trigger is IWiredResettableTimer resettable)
            {
                resettable.ResetTimer(_currentTickMs);
            }
        }
    }

    private async Task ProcessRoomEventAsync(RoomEvent evt, long now, CancellationToken ct)
    {
        if (
            evt is null
            || !_triggersByEventType.TryGetValue(
                evt.GetType(),
                out List<FurnitureWiredTriggerLogic>? triggers
            )
        )
        {
            return;
        }

        // Snapshot: firing an action can mutate room furniture, and a stale registry entry sets
        // _indexDirty; iterate a copy so that never disturbs this loop.
        foreach (FurnitureWiredTriggerLogic trigger in triggers.ToList())
        {
            if (!_roomGrain._state.ItemsById.ContainsKey(trigger.ObjectId))
            {
                _indexDirty = true;

                continue;
            }

            WiredStack stack = await BuildStackFromTileAsync(trigger.TileIdx, ct);

            await FireTriggerWithEventAsync(trigger, evt, stack, now, ct);
        }
    }

    private async Task FireTriggerWithEventAsync(
        IWiredTrigger trigger,
        RoomEvent evt,
        IWiredStack stack,
        long now,
        CancellationToken ct
    )
    {
        if (
            trigger is null
            || evt is null
            || stack is null
            || !await trigger.MatchesEventAsync(evt, ct)
        )
        {
            return;
        }

        WiredProcessingContext ctx = new(_roomGrain)
        {
            Event = evt,
            Stack = stack,
            Trigger = trigger,
        };

        if (evt.CausedBy.Origin == ActionOrigin.Player && evt.CausedBy.PlayerId > 0)
        {
            ctx.Selected.SelectedPlayerIds.Add(evt.CausedBy.PlayerId);
        }

        IWiredSelectionSet selection = await ctx.GetWiredSelectionSetAsync(trigger, ct);

        ctx.Selected.UnionWith(selection);

        foreach (IWiredSelector selector in ctx.Stack.Selectors)
        {
            IWiredSelectionSet set = await selector.SelectAsync(ctx, ct);

            ctx.SelectorPool.UnionWith(set);
        }

        foreach (IWiredAddon addon in ctx.Stack.Addons)
        {
            try
            {
                await addon.MutatePolicyAsync(ctx, ct);
            }
            catch (Exception ex)
            {
                _roomGrain._logger.LogWarning(
                    ex,
                    "Wired addon {AddonType} failed to mutate the policy in room {RoomId}.",
                    addon.GetType().Name,
                    _roomGrain.RoomId
                );
            }
        }

        if (!EvaluateConditions(ctx.Stack.Conditions, ctx))
        {
            return;
        }

        if (!await trigger.CanTriggerAsync(ctx, ct))
        {
            return;
        }

        _ = ctx.Trigger.FlashActivationStateAsync(ct);

        // Before/AfterEffects addon hooks run in ExecuteStackChainAsync, around the chain's actual
        // execution — which can be ticks later than this scheduling when actions carry delays.
        ScheduleStackExecution(ctx, now);
    }

    private void ScheduleStackExecution(WiredProcessingContext ctx, long dueAtMs)
    {
        // ctx.Stack was resolved live from the trigger's current tile, so every action in it is already
        // co-located with the trigger. Delayed actions are re-validated again at execution time in
        // ExecuteStackChainAsync, in case a box leaves the pile during its delay window.
        List<IWiredAction> actions = ChooseActions(ctx.Stack.Actions, ctx.Policy);

        if (actions.Count == 0)
        {
            return;
        }

        WiredExecutionKey key = new(
            ctx.Stack.StackId,
            Interlocked.Increment(ref _nextStackExecutionId)
        );

        WiredPendingStackExecution pending = new()
        {
            Stack = ctx.Stack,
            Actions = actions,
            Trigger = ctx.Trigger,
            Policy = ctx.Policy,
            Selected = ctx.Selected,
            SelectorPool = ctx.SelectorPool,
            ProcessingContext = ctx,
            Version = 1,
            DueAtMs = dueAtMs,
            NextActionIndex = 0,
        };

        _pendingStackExecutions[key] = pending;
        _stackSchedule.Enqueue((key, pending.Version), pending.DueAtMs);
    }

    private async Task RunDueScheduledStackExecutionsAsync(long now, CancellationToken ct)
    {
        int budget = _roomGrain._roomConfig.WiredMaxScheduledPerTick;

        while (budget-- > 0 && _stackSchedule.Count > 0)
        {
            ((WiredExecutionKey key, long version) entry, long dueAtMs) = PeekSchedule();

            if (dueAtMs > now)
            {
                break;
            }

            _stackSchedule.Dequeue();

            (WiredExecutionKey key, long version) = entry;

            if (
                !_pendingStackExecutions.TryGetValue(key, out WiredPendingStackExecution? pending)
                || pending.Version != version
            )
            {
                continue;
            }

            if (pending.DueAtMs > now)
            {
                continue;
            }

            if (await ExecuteStackChainAsync(key, pending, now, ct))
            {
                _pendingStackExecutions.Remove(key);
            }
        }

        ((WiredExecutionKey key, long version) entry, long dueAtMs) PeekSchedule()
        {
            if (_stackSchedule.TryPeek(out (WiredExecutionKey key, long version) k, out long p))
            {
                return (k, p);
            }

            return (default, long.MaxValue);
        }
    }

    private async Task<bool> ExecuteStackChainAsync(
        WiredExecutionKey key,
        WiredPendingStackExecution pending,
        long now,
        CancellationToken ct
    )
    {
        if (!pending.EffectsStarted)
        {
            pending.EffectsStarted = true;

            await RunAddonEffectHooksAsync(pending, before: true, ct);
        }

        for (int i = pending.NextActionIndex; i < pending.Actions.Count; i++)
        {
            IWiredAction action = pending.Actions[i];

            if (pending.WaitingActionIndex == i)
            {
                if (now < pending.DueAtMs)
                {
                    return false;
                }

                pending.WaitingActionIndex = null;
            }
            else
            {
                int delayMs = Math.Max(0, action.GetDelayMs());

                if (delayMs > 0)
                {
                    pending.WaitingActionIndex = i;

                    RescheduleStack(key, pending, now + delayMs);

                    return false;
                }
            }

            // Re-validate co-location at execution time. A zero-delay action was resolved live moments
            // ago, but a delayed action may have been dragged off the trigger's tile (or picked up)
            // during its delay window — Habbo only lets a trigger drive actions on its own pile, so
            // such an action must not fire. key.StackId is the tile the trigger fired from.
            if (
                action is FurnitureWiredLogic actionBox
                && (
                    !_roomGrain._state.ItemsById.ContainsKey(actionBox.ObjectId)
                    || !IsOnTile(actionBox.ObjectId, key.StackId)
                )
            )
            {
                pending.NextActionIndex = i + 1;

                continue;
            }

            try
            {
                WiredExecutionContext ctx = new(_roomGrain)
                {
                    Policy = pending.Policy,
                    Selected = new WiredSelectionSet().UnionWith(pending.Selected),
                    SelectorPool = new WiredSelectionSet().UnionWith(pending.SelectorPool),
                };

                _ = action.FlashActivationStateAsync(ct);

                await action.ExecuteAsync(ctx, ct);

                _ = FlushWiredContextAsync(ctx);

                WriteWiredRoomLog(
                    WiredLogLevel.Info,
                    WiredLogSource.Action,
                    $"Action {action.GetType().Name} executed for stack {key}."
                );
            }
            catch (Exception ex)
            {
                _roomGrain._logger.LogWarning(
                    ex,
                    "Failed to execute pending wired action {ActionIndex} for stack {StackKey} in room {RoomId}.",
                    i,
                    key,
                    _roomGrain.RoomId
                );

                RecordWiredErrorLog(ex, action, now);

                WriteWiredRoomLog(
                    WiredLogLevel.Error,
                    WiredLogSource.Action,
                    $"Action {action.GetType().Name} failed for stack {key}: {ex.GetType().Name}."
                );
            }

            pending.NextActionIndex = i + 1;
        }

        await RunAddonEffectHooksAsync(pending, before: false, ct);

        return true;
    }

    private async Task RunAddonEffectHooksAsync(
        WiredPendingStackExecution pending,
        bool before,
        CancellationToken ct
    )
    {
        foreach (IWiredAddon addon in pending.Stack.Addons)
        {
            try
            {
                if (before)
                {
                    await addon.BeforeEffectsAsync(pending.ProcessingContext, ct);
                }
                else
                {
                    await addon.AfterEffectsAsync(pending.ProcessingContext, ct);
                }
            }
            catch (Exception ex)
            {
                _roomGrain._logger.LogWarning(
                    ex,
                    "Wired addon {AddonType} {Hook} hook failed in room {RoomId}.",
                    addon.GetType().Name,
                    before ? "BeforeEffects" : "AfterEffects",
                    _roomGrain.RoomId
                );
            }
        }
    }

    /// <summary>Marks a wired box as lit; the wired tick reverts it to unlit after
    /// <c>WiredFlashDurationMs</c>. Re-flashing an already-lit box pushes its revert back.</summary>
    public void ScheduleFlashRevert(RoomObjectId objectId)
    {
        _flashRevertAtMs[objectId] = _currentTickMs + _roomGrain._roomConfig.WiredFlashDurationMs;
    }

    private async Task ProcessFlashRevertsAsync(long now, CancellationToken ct)
    {
        if (_flashRevertAtMs.Count == 0)
        {
            return;
        }

        List<RoomObjectId>? due = null;

        foreach ((RoomObjectId objectId, long revertAtMs) in _flashRevertAtMs)
        {
            if (revertAtMs <= now)
            {
                (due ??= []).Add(objectId);
            }
        }

        if (due is null)
        {
            return;
        }

        foreach (RoomObjectId objectId in due)
        {
            _flashRevertAtMs.Remove(objectId);

            // A box picked up (or replaced) while lit simply has no revert to apply.
            if (
                !_roomGrain._state.ItemsById.TryGetValue(objectId, out IRoomItem? item)
                || item.Logic is not FurnitureWiredLogic wiredLogic
            )
            {
                continue;
            }

            try
            {
                await wiredLogic.SetFlashStateAsync(0);
            }
            catch (Exception ex)
            {
                _roomGrain._logger.LogWarning(
                    ex,
                    "Failed to revert the flash state of wired item {ItemId} in room {RoomId}.",
                    objectId,
                    _roomGrain.RoomId
                );
            }
        }
    }

    private void RecordWiredErrorLog(Exception ex, IWiredAction action, long now)
    {
        string errorName = ex.GetType().Name;

        if (
            !_roomGrain._state.WiredErrorLogCounters.TryGetValue(
                errorName,
                out WiredErrorLogCounter? counter
            )
        )
        {
            counter = new WiredErrorLogCounter
            {
                ErrorName = errorName,
                Category = action.GetType().Name,
            };

            _roomGrain._state.WiredErrorLogCounters[errorName] = counter;
        }

        counter.ThrowCount++;
        counter.LastOccurrenceMs = now;
    }

    private void WriteWiredRoomLog(WiredLogLevel level, WiredLogSource source, string message)
    {
        _roomGrain._wiredLogChannel.TryWrite(
            new RoomWiredLogEntry
            {
                RoomId = _roomGrain.RoomId.Value,
                LogLevel = level,
                LogSource = source,
                Message = message,
            }
        );
    }

    private void RescheduleStack(
        WiredExecutionKey key,
        WiredPendingStackExecution pending,
        long dueAtMs
    )
    {
        if (pending.DueAtMs != dueAtMs)
        {
            pending.Version++;
        }

        pending.DueAtMs = dueAtMs;

        _pendingStackExecutions[key] = pending;
        _stackSchedule.Enqueue((key, pending.Version), pending.DueAtMs);
    }

    private Task FlushWiredContextAsync(WiredExecutionContext ctx)
    {
        if (
            ctx.UserMoves.Count > 0
            || ctx.UserDirections.Count > 0
            || ctx.FloorItemMoves.Count > 0
            || ctx.WallItemMoves.Count > 0
        )
        {
            _ = ctx.SendComposerToRoomAsync(
                new WiredMovementsMessageComposer
                {
                    Users = ctx.UserMoves,
                    FloorItems = ctx.FloorItemMoves,
                    WallItems = ctx.WallItemMoves,
                    UserDirections = ctx.UserDirections,
                }
            );
        }

        if (ctx.FloorItemStateUpdates.Count > 0)
        {
            _ = ctx.SendComposerToRoomAsync(
                new ObjectsDataUpdateMessageComposer { StuffDatas = ctx.FloorItemStateUpdates }
            );
        }

        if (ctx.WallItemStateUpdates.Count > 0)
        {
            _ = ctx.SendComposerToRoomAsync(
                new ItemsStateUpdateMessageComposer { ObjectStates = ctx.WallItemStateUpdates }
            );
        }

        return Task.CompletedTask;
    }

    private async Task RebuildTriggerIndexAsync(CancellationToken ct)
    {
        _triggersByEventType.Clear();
        _timedTriggers.Clear();

        foreach (IRoomItem item in _roomGrain._state.ItemsById.Values)
        {
            if (item.Logic is not FurnitureWiredTriggerLogic trigger)
            {
                continue;
            }

            try
            {
                // Hydrate so timed triggers have their schedule ready for polling this tick.
                await trigger.LoadWiredAsync(ct);
            }
            catch (Exception ex)
            {
                _roomGrain._logger.LogWarning(
                    ex,
                    "Failed to hydrate wired trigger {ItemId} in room {RoomId}.",
                    item.ObjectId,
                    _roomGrain.RoomId
                );

                continue;
            }

            if (trigger is IWiredTimedTrigger)
            {
                _timedTriggers.Add(trigger);
            }

            foreach (Type eventType in trigger.SupportedEventTypes)
            {
                if (
                    !_triggersByEventType.TryGetValue(
                        eventType,
                        out List<FurnitureWiredTriggerLogic>? list
                    )
                )
                {
                    list = [];
                    _triggersByEventType[eventType] = list;
                }

                list.Add(trigger);
            }
        }
    }

    /// <summary>
    /// Resolves the wired pile physically stacked on <paramref name="tileIdx"/> right now, classifying
    /// each co-located wired box into the trigger / selector / condition / addon / action buckets of a
    /// fresh <see cref="WiredStack"/>. Called at fire time so the pile is always live truth — a box
    /// dragged off the tile or picked up simply is not in the result, which is exactly the Habbo rule
    /// that a trigger only drives the boxes stacked with it. Members are ordered by object id so effect
    /// execution is deterministic (physical stack order is irrelevant in Habbo).
    /// </summary>
    private async Task<WiredStack> BuildStackFromTileAsync(int tileIdx, CancellationToken ct)
    {
        WiredStack stack = new() { StackId = tileIdx };

        if (tileIdx < 0 || tileIdx >= _roomGrain._state.TileFloorStacks.Length)
        {
            return stack;
        }

        foreach (
            RoomObjectId id in _roomGrain._state.TileFloorStacks[tileIdx].OrderBy(x => x.Value)
        )
        {
            if (
                !_roomGrain._state.ItemsById.TryGetValue(id, out IRoomItem? item)
                || item.Logic is not FurnitureWiredLogic wiredLogic
                || wiredLogic is FurnitureWiredVariableLogic
            )
            {
                continue;
            }

            try
            {
                await wiredLogic.LoadWiredAsync(ct);

                switch (wiredLogic)
                {
                    case FurnitureWiredTriggerLogic trigger:
                        stack.Triggers.Add(trigger);
                        break;
                    case FurnitureWiredSelectorLogic selector:
                        stack.Selectors.Add(selector);
                        break;
                    case FurnitureWiredConditionLogic condition:
                        stack.Conditions.Add(condition);
                        break;
                    case FurnitureWiredAddonLogic addon:
                        stack.Addons.Add(addon);
                        break;
                    case FurnitureWiredActionLogic effect:
                        stack.Actions.Add(effect);
                        break;
                }
            }
            catch (Exception ex)
            {
                _roomGrain._logger.LogWarning(
                    ex,
                    "Failed to load wired logic for item {ItemId} in room {RoomId}.",
                    item.ObjectId,
                    _roomGrain.RoomId
                );
            }
        }

        return stack;
    }

    /// <summary>True if the given object currently sits on <paramref name="tileIdx"/>'s floor pile.</summary>
    private bool IsOnTile(RoomObjectId objectId, int tileIdx) =>
        tileIdx >= 0
        && tileIdx < _roomGrain._state.TileFloorStacks.Length
        && _roomGrain._state.TileFloorStacks[tileIdx].Contains(objectId);

    private static List<IWiredAction> ChooseActions(List<IWiredAction> actions, IWiredPolicy policy)
    {
        if (actions.Count == 0)
        {
            return [];
        }

        return policy.EffectMode switch
        {
            WiredEffectModeType.FirstOnly => [actions[0]],
            WiredEffectModeType.Random => [actions[Random.Shared.Next(actions.Count)]],
            _ => [.. actions],
        };
    }

    private static bool EvaluateConditions(
        List<IWiredCondition> conditions,
        WiredProcessingContext ctx
    )
    {
        if (conditions.Count == 0)
        {
            return true;
        }

        return ctx.Policy.ConditionMode switch
        {
            WiredConditionModeType.None => true,
            WiredConditionModeType.Any => conditions.Exists(c => c.Evaluate(ctx)),
            WiredConditionModeType.All => conditions.TrueForAll(c => c.Evaluate(ctx)),
            _ => conditions.TrueForAll(c => c.Evaluate(ctx)),
        };
    }
}
