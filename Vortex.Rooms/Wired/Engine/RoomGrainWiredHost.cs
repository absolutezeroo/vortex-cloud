using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Extensions.Logging;
using Vortex.Primitives.Rooms;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Rooms.Grains;
using Vortex.Rooms.Wired.Logs;

namespace Vortex.Rooms.Wired.Engine;

/// <summary>
/// The room, as the wired engine is allowed to see it. Every member here is a read the engine used to
/// perform by reaching into <c>RoomGrain</c>'s fields directly.
/// </summary>
/// <remarks>
/// It runs inside the room's turn, like everything else the grain composes, so the sequences it
/// materialises cannot be observed half-built. Materialising them is not about safety from
/// concurrency — Orleans already gives that — but about ownership: a component holding the room's own
/// dictionary could mutate it from outside the ordering the grain maintains, and nothing would say
/// so.
/// </remarks>
internal sealed class RoomGrainWiredHost(RoomGrain roomGrain)
    : IWiredRoomHost,
        IWiredRoomView,
        IWiredDiagnostics
{
    private readonly RoomGrain _roomGrain = roomGrain;

    public IWiredRoomView View => this;

    public IWiredDiagnostics Diagnostics => this;

    public IReadOnlyList<IWiredVariable> InternalVariables() =>
        [.. _roomGrain._wiredVariablesProvider.BuildVariablesForRoom(_roomGrain)];

    public RoomId RoomId => _roomGrain.RoomId;

    public long NowMs() => _roomGrain.NowMs();

    public long AdvanceBoundaryPast(long now, int periodMs) =>
        _roomGrain.AdvanceBoundaryPast(now, periodMs);

    public long NextWiredBoundaryMs
    {
        get => _roomGrain._state.NextWiredBoundaryMs;
        set => _roomGrain._state.NextWiredBoundaryMs = value;
    }

    public WiredVariableHash AllVariablesHash
    {
        get => _roomGrain._state.AllVariablesHash;
        set => _roomGrain._state.AllVariablesHash = value;
    }

    public IWiredLimits Limits => _roomGrain._roomConfig;

    public int MaxCallChainDepth => _roomGrain._roomConfig.WiredMaxDepth;

    public int WiredTickMs => _roomGrain._roomConfig.WiredTickMs;

    public int MaxQueuedEvents => _roomGrain._roomConfig.WiredMaxQueuedEvents;

    public int MaxEventsPerTick => _roomGrain._roomConfig.WiredMaxEventsPerTick;

    public int MaxScheduledPerTick => _roomGrain._roomConfig.WiredMaxScheduledPerTick;

    public int FlashDurationMs => _roomGrain._roomConfig.WiredFlashDurationMs;

    public bool TryGetItem(RoomObjectId objectId, [NotNullWhen(true)] out IRoomItem? item) =>
        _roomGrain._state.ItemsById.TryGetValue(objectId, out item);

    public bool HasItem(RoomObjectId objectId) => _roomGrain._state.ItemsById.ContainsKey(objectId);

    public IReadOnlyList<IRoomItem> AllItems() => [.. _roomGrain._state.ItemsById.Values];

    public IReadOnlyList<int> AllItemIds() =>
        [.. _roomGrain._state.ItemsById.Keys.Select(i => i.Value)];

    public IReadOnlyList<int> AllAvatarPlayerIds() =>
        [.. _roomGrain._state.AvatarsByPlayerId.Keys.Select(p => p.Value)];

    public int TileCount => _roomGrain._state.TileFloorStacks.Length;

    public IReadOnlyList<IRoomFloorItem> EnumerateTileFloorStack(int tileIdx)
    {
        if (tileIdx < 0 || tileIdx >= _roomGrain._state.TileFloorStacks.Length)
        {
            return [];
        }

        List<IRoomFloorItem> stack = [];

        // Ordered by object id: that is the order a pile resolves in, and it is the room's own rule
        // rather than this host's, so it is applied here where the stack is read.
        foreach (
            RoomObjectId id in _roomGrain._state.TileFloorStacks[tileIdx].OrderBy(x => x.Value)
        )
        {
            if (
                _roomGrain._state.ItemsById.TryGetValue(id, out IRoomItem? item)
                && item is IRoomFloorItem floor
            )
            {
                stack.Add(floor);
            }
        }

        return stack;
    }

    public bool IsOnTile(int tileIdx, RoomObjectId objectId) =>
        tileIdx >= 0
        && tileIdx < _roomGrain._state.TileFloorStacks.Length
        && _roomGrain._state.TileFloorStacks[tileIdx].Contains(objectId);

    public int ToIdx(int x, int y) => _roomGrain.MapModule.ToIdx(x, y);

    public ILogger Logger => _roomGrain._logger;

    public void ChainStopped(string reason) => _roomGrain._metrics.WiredChainStopped(reason);

    public void WriteRoomLog(RoomWiredLogEntry entry) =>
        _roomGrain._wiredLogChannel.TryWrite(entry);

    public void RecordError(string errorName, string category, long nowMs)
    {
        if (
            !_roomGrain._state.WiredErrorLogCounters.TryGetValue(
                errorName,
                out WiredErrorLogCounter? counter
            )
        )
        {
            counter = new WiredErrorLogCounter { ErrorName = errorName, Category = category };

            _roomGrain._state.WiredErrorLogCounters[errorName] = counter;
        }

        counter.ThrowCount++;
        counter.LastOccurrenceMs = nowMs;
    }
}
