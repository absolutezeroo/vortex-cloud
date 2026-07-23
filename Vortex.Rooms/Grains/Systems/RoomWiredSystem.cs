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
    private readonly HashSet<int> _dirtyStackIds = [];

    private readonly Queue<RoomEvent> _eventQueue = new();

    private readonly Dictionary<
        WiredExecutionKey,
        WiredPendingStackExecution
    > _pendingStackExecutions = [];

    private readonly RoomGrain _roomGrain = roomGrain;
    private readonly Dictionary<Type, List<int>> _stackIdsByEventType = [];
    private readonly Dictionary<int, IWiredStack> _stacksById = [];

    private readonly PriorityQueue<(WiredExecutionKey key, long version), long> _stackSchedule =
        new();

    private bool _firstRun = true;
    private long _nextStackExecutionId;

    private int _tickMs => _roomGrain._roomConfig.WiredTickMs;

    public Task OnRoomEventAsync(RoomEvent evt, CancellationToken ct)
    {
        if (evt is null)
        {
            return Task.CompletedTask;
        }

        switch (evt)
        {
            case RoomWiredStackChangedEvent stackEvt:
                {
                    foreach (int stackId in stackEvt.StackIds)
                    {
                        _dirtyStackIds.Add(stackId);
                    }
                }
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
                _eventQueue.Enqueue(evt);
                break;
            case RoomItemDetachedEvent detatchedEvt:
                _furnitureActiveStore.RemoveFurnitureStore(detatchedEvt.ObjectId);
                break;
            default:
                _eventQueue.Enqueue(evt);
                break;
        }

        return Task.CompletedTask;
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

        if (_firstRun)
        {
            await ProcessInternalVariablesAsync(now, ct);

            _firstRun = false;
        }

        await ProcessVariableBoxesAsync(now, ct);
        await ProcessWiredStacksAsync(now, ct);
        await RunDueScheduledStackExecutionsAsync(now, ct);

        if (_stacksById.Count == 0 || _stackIdsByEventType.Count == 0)
        {
            return;
        }

        await ProcessTimedTriggersAsync(now, ct);
        await ProcessCountersAsync(now, ct);

        int budget = _roomGrain._roomConfig.WiredMaxEventsPerTick;

        while (budget-- > 0 && _eventQueue.Count > 0)
        {
            RoomEvent evt = _eventQueue.Dequeue();

            await ProcessRoomEventAsync(evt, now, ct);
        }

        //await RunDueScheduledStackExecutionsAsync(now, ct);
    }

    private async Task ProcessTimedTriggersAsync(long now, CancellationToken ct)
    {
        foreach (IWiredStack stack in _stacksById.Values)
        {
            foreach (IWiredTrigger trigger in stack.Triggers)
            {
                if (trigger is not IWiredTimedTrigger timed || !timed.TryConsumeDue(now))
                {
                    continue;
                }

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
    }

    private async Task ProcessCountersAsync(long now, CancellationToken ct)
    {
        foreach (IWiredStack stack in _stacksById.Values)
        {
            foreach (IWiredTrigger trigger in stack.Triggers)
            {
                if (trigger is not IWiredCounter counter || !counter.AdvanceClock(now))
                {
                    continue;
                }

                // The countdown just hit zero — fire the counter's CLOCK_REACH_TIME wired.
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
    }

    private async Task ProcessRoomEventAsync(RoomEvent evt, long now, CancellationToken ct)
    {
        if (
            evt is null
            || !_stackIdsByEventType.TryGetValue(evt.GetType(), out List<int>? stackIds)
        )
        {
            return;
        }

        foreach (int stackId in stackIds)
        {
            if (!_stacksById.TryGetValue(stackId, out IWiredStack? stack) || stack is null)
            {
                continue;
            }

            foreach (IWiredTrigger trigger in stack.Triggers)
            {
                await FireTriggerWithEventAsync(trigger, evt, stack, now, ct);
            }
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
            await addon.MutatePolicyAsync(ctx, ct);
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

        foreach (IWiredAddon addon in ctx.Stack.Addons)
        {
            await addon.BeforeEffectsAsync(ctx, ct);
        }

        ScheduleStackExecution(ctx, now, ct);

        foreach (IWiredAddon addon in ctx.Stack.Addons)
        {
            await addon.AfterEffectsAsync(ctx, ct);
        }
    }

    private void ScheduleStackExecution(
        WiredProcessingContext ctx,
        long dueAtMs,
        CancellationToken ct
    )
    {
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

        return true;
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

    private async Task ProcessWiredStacksAsync(long now, CancellationToken ct)
    {
        if (_dirtyStackIds.Count == 0)
        {
            return;
        }

        List<int> dirtyStackIds = _dirtyStackIds.ToList();
        _dirtyStackIds.Clear();

        foreach (int stackId in dirtyStackIds)
        {
            await ProcessWiredStackAsync(stackId, ct);
        }

        _stackIdsByEventType.Clear();

        foreach (IWiredStack stack in _stacksById.Values)
        {
            foreach (IWiredTrigger trigger in stack.Triggers)
            {
                foreach (Type eventType in trigger.SupportedEventTypes)
                {
                    if (!_stackIdsByEventType.TryGetValue(eventType, out List<int>? list))
                    {
                        list = [];
                        _stackIdsByEventType[eventType] = list;
                    }

                    list.Add(stack.StackId);
                }
            }
        }
    }

    private async Task ProcessWiredStackAsync(int stackId, CancellationToken ct)
    {
        _stacksById.Remove(stackId);

        List<IRoomItem> wiredItems = _roomGrain
            ._state.TileFloorStacks[stackId]
            .Select(x => _roomGrain._state.ItemsById[x])
            .Where(x =>
                x.Logic is FurnitureWiredLogic && x.Logic is not FurnitureWiredVariableLogic
            )
            .ToList();

        if (wiredItems.Count == 0)
        {
            return;
        }

        WiredStack stack = new() { StackId = stackId };

        foreach (IRoomItem item in wiredItems)
        {
            try
            {
                FurnitureWiredLogic wiredLogic = (FurnitureWiredLogic)item.Logic!;
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

        _stacksById[stackId] = stack;
    }

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
