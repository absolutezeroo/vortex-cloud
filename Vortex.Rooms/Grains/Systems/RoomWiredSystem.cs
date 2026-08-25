using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Vortex.Logging.Extensions;
using Vortex.Primitives.Action;
using Vortex.Primitives.Observability;
using Vortex.Primitives.Rooms;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Events;
using Vortex.Primitives.Rooms.Events.Player;
using Vortex.Primitives.Rooms.Events.RoomItem;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Protocol.Messages.Outgoing.Room.Engine;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Actions;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Addons;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Conditions;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Selectors;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Triggers;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Variables;
using Vortex.Rooms.Wired;
using Vortex.Rooms.Wired.Engine;
using Vortex.Rooms.Wired.Logs;

namespace Vortex.Rooms.Grains.Systems;

public sealed partial class RoomWiredSystem : IRoomEventListener
{
    public RoomWiredSystem(RoomGrain roomGrain)
    {
        _roomGrain = roomGrain;
        _host = new RoomGrainWiredHost(roomGrain);
        _triggers = new WiredTriggerIndex(_host.View, _host.Diagnostics);
        _stacks = new WiredStackResolver(_host.View, _host.Diagnostics);

        // Random.Shared would do, and did — but a pile drawing "two effects, avoiding the last
        // three firings" has behaviour worth pinning, and pinning it needs a sequence a test can
        // predict.
        _policy = new WiredExecutionPolicy(_host.Diagnostics, Random.Shared);
    }

    private readonly Queue<RoomEvent> _eventQueue = new();

    private readonly Dictionary<
        WiredExecutionKey,
        WiredPendingStackExecution
    > _pendingStackExecutions = [];

    private readonly RoomGrain _roomGrain;

    // Everything the engine needs from the room, and nothing else. It used to reach into the grain's
    // fields; going through the host is what will let the pipeline be tested without building most
    // of a room (the leaves have always been testable, the orchestrator never was).
    private readonly IWiredRoomHost _host;

    private IWiredRoomView Room => _host.View;

    private IWiredDiagnostics Diagnostics => _host.Diagnostics;

    // Which trigger boxes are in the room and what they listen for, and how a tile's pile is
    // resolved. Both read the room through the host, so both can be exercised without one.
    private readonly WiredTriggerIndex _triggers;
    private readonly WiredStackResolver _stacks;
    private readonly WiredExecutionPolicy _policy;

    private readonly PriorityQueue<(WiredExecutionKey key, long version), long> _stackSchedule =
        new();

    private bool _firstRun = true;
    private long _nextStackExecutionId;

    // Tiles whose pile is somewhere in the current "execute stacks" chain. A pile that calls itself,
    // or two piles that call each other, would otherwise recurse until the room fell over; a tile
    // already in this set is skipped rather than entered twice.
    private readonly HashSet<int> _callChainTiles = [];

    /// <summary>
    /// How deep one "execute stacks" chain may go. The tile guard already makes a cycle impossible;
    /// this bounds the cost of a wide, legitimate chain.
    /// <para>
    /// It used to be a private const of 8 while <c>RoomConfig.WiredMaxDepth</c> said 20 and was read
    /// by nothing (RFW-101): an operator raising the setting changed the room's behaviour not at
    /// all. The setting is the source of truth now, and its default was lowered to 8 rather than the
    /// engine's ceiling being raised to 20 — 8 is what every room has actually been running, and the
    /// depth Habbo itself allows is <c>UNKNOWN</c> (OQ-1). Changing the value and changing where it
    /// is read are two decisions; this is only the second.
    /// </para>
    /// </summary>
    private int MaxCallChainDepth => Room.MaxCallChainDepth;

    // Boxes currently lit by FlashActivationStateAsync, mapped to the room-clock time their visual
    // state reverts to unlit. Re-flashing a box simply pushes its revert time back.
    private readonly Dictionary<RoomObjectId, long> _flashRevertAtMs = [];

    // Events rejected because the queue hit WiredMaxQueuedEvents, reported once per tick in the
    // room's wired log instead of spamming one entry per drop.
    private int _droppedEventCount;

    // The room-clock time of the tick currently being processed, so an executing action (e.g. the Timer
    // Reset effect) can re-anchor schedules to "now" without threading the value through every call.
    private long _currentTickMs;

    private int _tickMs => Room.WiredTickMs;

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
                _triggers.MarkDirty();
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
        if (!_triggers.IsDirty && !_triggers.Listens(evt.GetType()))
        {
            return;
        }

        // WiredMaxEventsPerTick bounds the tick's work; this bounds the queue's memory under a
        // sustained storm. Rejecting the incoming event (rather than evicting an older one) keeps
        // trigger ordering intact for what was already accepted.
        if (_eventQueue.Count >= Room.MaxQueuedEvents)
        {
            _droppedEventCount++;
            Diagnostics.ChainStopped(WiredStopReason.QUEUE_DROP);

            return;
        }

        _eventQueue.Enqueue(evt);
    }

    public async Task ProcessWiredAsync(long now, CancellationToken ct)
    {
        if (now < Room.NextWiredBoundaryMs)
        {
            return;
        }

        Room.NextWiredBoundaryMs = Room.AdvanceBoundaryPast(now, _tickMs);

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

        if (_triggers.IsDirty)
        {
            await _triggers.RebuildAsync(ct);
        }

        // Run action chains scheduled on earlier ticks that are now due (delayed effects resuming).
        await RunDueScheduledStackExecutionsAsync(now, ct);

        if (_triggers.IsEmpty)
        {
            // No wired triggers in the room: nothing can consume queued room events, so drop them
            // rather than let the queue grow unbounded.
            _eventQueue.Clear();

            return;
        }

        await ProcessTimedTriggersAsync(now, ct);

        int budget = Room.MaxEventsPerTick;

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
        for (int i = 0; i < _triggers.Timed.Count; i++)
        {
            FurnitureWiredTriggerLogic trigger = _triggers.Timed[i];

            if (trigger is not IWiredTimedTrigger timed)
            {
                continue;
            }

            // A box lingering in the registry after being picked up: skip it and reindex next tick.
            if (!Room.HasItem(trigger.ObjectId))
            {
                _triggers.MarkDirty();

                continue;
            }

            if (!timed.TryConsumeDue(now))
            {
                continue;
            }

            // Resolve the pile the trigger currently sits on, live. If it was dragged onto an empty
            // tile the pile has no actions and nothing fires — the "same pile" rule, for free.
            WiredStack stack = await _stacks.BuildFromTileAsync(trigger.TileIdx, ct);

            await FireTriggerWithEventAsync(
                trigger,
                new PeriodicRoomEvent
                {
                    RoomId = Room.RoomId,
                    CausedBy = ActionContext.CreateForWired(Room.RoomId),
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
        foreach (FurnitureWiredTriggerLogic trigger in _triggers.Timed)
        {
            if (trigger is IWiredResettableTimer resettable)
            {
                resettable.ResetTimer(_currentTickMs);
            }
        }
    }

    private async Task ProcessRoomEventAsync(RoomEvent evt, long now, CancellationToken ct)
    {
        if (evt is null)
        {
            return;
        }

        // A snapshot, because firing an action can mutate room furniture and a stale registry entry
        // marks the index dirty — iterating the live list would be iterating something this loop is
        // changing.
        foreach (FurnitureWiredTriggerLogic trigger in _triggers.Listening(evt.GetType()))
        {
            if (!Room.HasItem(trigger.ObjectId))
            {
                _triggers.MarkDirty();

                continue;
            }

            WiredStack stack = await _stacks.BuildFromTileAsync(trigger.TileIdx, ct);

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
            Signal = BuildSignalSelection(evt),
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
                Diagnostics.Logger.LogWarning(
                    ex,
                    "Wired addon {AddonType} failed to mutate the policy in room {RoomId}.",
                    addon.GetType().Name,
                    Room.RoomId
                );
            }
        }

        foreach (IWiredCondition condition in ctx.Stack.Conditions)
        {
            try
            {
                await condition.PrepareAsync(ctx, ct);
            }
            catch (Exception ex)
            {
                Diagnostics.Logger.LogWarning(
                    ex,
                    "Wired condition {ConditionType} failed to prepare in room {RoomId}; it will be evaluated without its data.",
                    condition.GetType().Name,
                    Room.RoomId
                );
            }
        }

        // The trigger is asked before the conditions, not after: a pile's negative actions are for
        // "the trigger fired but the conditions did not hold", so the two answers have to be
        // distinguishable. It also means a trigger that seals the triggering user into the
        // selection has done so before the conditions read it.
        if (!await trigger.CanTriggerAsync(ctx, ct))
        {
            return;
        }

        // The limit is asked after the add-ons have spoken (they are what sets it) and before any
        // branch runs, so a pile that is over its quota costs nothing at all this firing.
        if (!_policy.TryConsumeAllowance(ctx.Stack.StackId, ctx.Policy, now))
        {
            return;
        }

        bool conditionsPassed = EvaluateConditions(ctx.Stack.Conditions, ctx);

        ctx.Trigger?.FlashActivationStateAsync(ct)
            .LogAndForget(Diagnostics.Logger, "Failed to flash activation state for trigger.");

        // Before/AfterEffects addon hooks run in ExecuteStackChainAsync, around the chain's actual
        // execution — which can be ticks later than this scheduling when actions carry delays.
        ScheduleStackExecution(ctx, now, conditionsPassed);
    }

    /// <summary>Builds the signal payload for a fired stack: the furni and users a receive-signal
    /// trigger was handed by the send-signal action. Empty for every other event.</summary>
    private static WiredSelectionSet BuildSignalSelection(RoomEvent evt)
    {
        WiredSelectionSet signal = new();

        if (evt is SignalRoomEvent signalEvt)
        {
            signal.SelectedFurniIds.UnionWith(signalEvt.FurniIds);
            signal.SelectedPlayerIds.UnionWith(signalEvt.PlayerIds);
        }

        return signal;
    }

    private void ScheduleStackExecution(
        WiredProcessingContext ctx,
        long dueAtMs,
        bool conditionsPassed
    )
    {
        // ctx.Stack was resolved live from the trigger's current tile, so every action in it is already
        // co-located with the trigger. Delayed actions are re-validated again at execution time in
        // ExecuteStackChainAsync, in case a box leaves the pile during its delay window.
        List<IWiredAction> actions = _policy.ChooseActions(
            ctx.Stack.StackId,
            WiredActionBranch.Select(ctx.Stack.Actions, conditionsPassed),
            ctx.Policy
        );

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
            Signal = ctx.Signal,
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
        int budget = Room.MaxScheduledPerTick;

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
                && (!Room.HasItem(actionBox.ObjectId) || !IsOnTile(actionBox.ObjectId, key.StackId))
            )
            {
                pending.NextActionIndex = i + 1;

                continue;
            }

            try
            {
                WiredExecutionContext ctx = new(_roomGrain)
                {
                    Addons = pending.Stack.Addons,
                    Policy = pending.Policy,
                    Selected = new WiredSelectionSet().UnionWith(pending.Selected),
                    SelectorPool = new WiredSelectionSet().UnionWith(pending.SelectorPool),
                    Signal = new WiredSelectionSet().UnionWith(pending.Signal),
                };

                action
                    .FlashActivationStateAsync(ct)
                    .LogAndForget(
                        Diagnostics.Logger,
                        "Failed to flash activation state for action."
                    );

                await action.ExecuteAsync(ctx, ct);

                FlushWiredContextAsync(ctx)
                    .LogAndForget(Diagnostics.Logger, "Failed to flush wired execution context.");

                WriteWiredRoomLog(
                    WiredLogLevel.Info,
                    WiredLogSource.Action,
                    $"Action {action.GetType().Name} executed for stack {key}."
                );
            }
            catch (Exception ex)
            {
                Diagnostics.Logger.LogWarning(
                    ex,
                    "Failed to execute pending wired action {ActionIndex} for stack {StackKey} in room {RoomId}.",
                    i,
                    key,
                    Room.RoomId
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
                Diagnostics.Logger.LogWarning(
                    ex,
                    "Wired addon {AddonType} {Hook} hook failed in room {RoomId}.",
                    addon.GetType().Name,
                    before ? "BeforeEffects" : "AfterEffects",
                    Room.RoomId
                );
            }
        }
    }

    /// <summary>Marks a wired box as lit; the wired tick reverts it to unlit after
    /// <c>WiredFlashDurationMs</c>. Re-flashing an already-lit box pushes its revert back.</summary>
    public void ScheduleFlashRevert(RoomObjectId objectId)
    {
        _flashRevertAtMs[objectId] = _currentTickMs + Room.FlashDurationMs;
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
                !Room.TryGetItem(objectId, out IRoomItem? item)
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
                Diagnostics.Logger.LogWarning(
                    ex,
                    "Failed to revert the flash state of wired item {ItemId} in room {RoomId}.",
                    objectId,
                    Room.RoomId
                );
            }
        }
    }

    private void RecordWiredErrorLog(Exception ex, IWiredAction action, long now) =>
        Diagnostics.RecordError(ex.GetType().Name, action.GetType().Name, now);

    private void WriteWiredRoomLog(WiredLogLevel level, WiredLogSource source, string message)
    {
        Diagnostics.WriteRoomLog(
            new RoomWiredLogEntry
            {
                RoomId = Room.RoomId.Value,
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
            ctx.SendComposerToRoomAsync(
                    new WiredMovementsMessageComposer
                    {
                        Users = ctx.UserMoves,
                        FloorItems = ctx.FloorItemMoves,
                        WallItems = ctx.WallItemMoves,
                        UserDirections = ctx.UserDirections,
                    }
                )
                .LogAndForget(Diagnostics.Logger, "Failed to broadcast wired movements.");
        }

        if (ctx.FloorItemStateUpdates.Count > 0)
        {
            ctx.SendComposerToRoomAsync(
                    new ObjectsDataUpdateMessageComposer { StuffDatas = ctx.FloorItemStateUpdates }
                )
                .LogAndForget(Diagnostics.Logger, "Failed to broadcast floor item state updates.");
        }

        if (ctx.WallItemStateUpdates.Count > 0)
        {
            ctx.SendComposerToRoomAsync(
                    new ItemsStateUpdateMessageComposer { ObjectStates = ctx.WallItemStateUpdates }
                )
                .LogAndForget(Diagnostics.Logger, "Failed to broadcast wall item state updates.");
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Runs the piles sitting under the given furni, as the "execute stacks" action asks: their
    /// triggers and their conditions are bypassed entirely, which is what the furni promises in as
    /// many words.
    /// </summary>
    /// <remarks>
    /// The calling pile's selection carries into the called one before its own selectors run, so
    /// "the user who walked on the first pile" is still who the second pile acts on. The caller's
    /// own tile is held in the chain guard for the duration, so a pile cannot execute itself.
    /// </remarks>
    /// <returns>How many piles were actually executed.</returns>
    internal async Task<int> ExecuteStacksAtAsync(
        int callerTileIdx,
        IReadOnlyCollection<int> targetFurniIds,
        IWiredSelectionSet inheritedSelection,
        CancellationToken ct
    )
    {
        if (targetFurniIds.Count == 0)
        {
            return 0;
        }

        if (_callChainTiles.Count >= MaxCallChainDepth)
        {
            Diagnostics.ChainStopped(WiredStopReason.DEPTH);

            return 0;
        }

        bool holdsCaller = callerTileIdx >= 0 && _callChainTiles.Add(callerTileIdx);
        int executed = 0;

        try
        {
            foreach (int furniId in targetFurniIds)
            {
                if (
                    !Room.TryGetItem(furniId, out IRoomItem? item)
                    || item is not IRoomFloorItem floor
                )
                {
                    continue;
                }

                int tileIdx = Room.ToIdx(floor.X, floor.Y);

                if (!_callChainTiles.Add(tileIdx))
                {
                    Diagnostics.ChainStopped(WiredStopReason.CYCLE);

                    continue;
                }

                try
                {
                    if (await ExecuteCalledStackAsync(tileIdx, inheritedSelection, ct))
                    {
                        executed++;
                    }
                }
                finally
                {
                    _callChainTiles.Remove(tileIdx);
                }
            }
        }
        finally
        {
            if (holdsCaller)
            {
                _callChainTiles.Remove(callerTileIdx);
            }
        }

        return executed;
    }

    private async Task<bool> ExecuteCalledStackAsync(
        int tileIdx,
        IWiredSelectionSet inheritedSelection,
        CancellationToken ct
    )
    {
        WiredStack stack = await _stacks.BuildFromTileAsync(tileIdx, ct);

        if (stack.Actions.Count == 0)
        {
            return false;
        }

        WiredProcessingContext ctx = new(_roomGrain)
        {
            // No trigger and no triggering event: this pile ran because another one said so.
            Event = new WiredStackCalledEvent
            {
                RoomId = Room.RoomId,
                CausedBy = ActionContext.CreateForWired(Room.RoomId),
            },
            Stack = stack,
            Trigger = null,
        };

        ctx.Selected.UnionWith(inheritedSelection);

        foreach (IWiredSelector selector in stack.Selectors)
        {
            ctx.SelectorPool.UnionWith(await selector.SelectAsync(ctx, ct));
        }

        foreach (IWiredAddon addon in stack.Addons)
        {
            try
            {
                await addon.MutatePolicyAsync(ctx, ct);
            }
            catch (Exception ex)
            {
                Diagnostics.Logger.LogWarning(
                    ex,
                    "Wired addon {AddonType} failed to mutate the policy of a called pile in room {RoomId}.",
                    addon.GetType().Name,
                    Room.RoomId
                );
            }
        }

        // Conditions are deliberately not evaluated, so the positive branch is what runs.
        ScheduleStackExecution(ctx, Room.NowMs(), conditionsPassed: true);

        return true;
    }

    /// <summary>True if the given object currently sits on <paramref name="tileIdx"/>'s floor pile.</summary>
    private bool IsOnTile(RoomObjectId objectId, int tileIdx) =>
        _stacks.IsOnTile(objectId, tileIdx);

    private static bool EvaluateConditions(
        List<IWiredCondition> conditions,
        WiredProcessingContext ctx
    )
    {
        if (conditions.Count == 0)
        {
            return true;
        }

        // Every condition is evaluated, not short-circuited: the counting modes need the exact number
        // that passed, and conditions are pure predicates over room state.
        int matched = 0;

        foreach (IWiredCondition condition in conditions)
        {
            if (condition.Evaluate(ctx))
            {
                matched++;
            }
        }

        int total = conditions.Count;
        int target = ctx.Policy.ConditionCompareValue;

        return ctx.Policy.ConditionMode switch
        {
            WiredConditionModeType.All => matched == total,
            WiredConditionModeType.AtLeastOne => matched > 0,
            WiredConditionModeType.NotAll => matched < total,
            WiredConditionModeType.None => matched == 0,
            WiredConditionModeType.CountLessThan => matched < target,
            WiredConditionModeType.CountExactly => matched == target,
            WiredConditionModeType.CountMoreThan => matched > target,
            _ => matched == total,
        };
    }
}
