using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Vortex.Primitives.Rooms;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Rooms.Wired.Engine;
using Vortex.Rooms.Wired.Logs;

namespace Vortex.Rooms.Tests.Wired;

/// <summary>
/// A room for the wired engine to run in, with no grain behind it.
/// </summary>
/// <remarks>
/// This is the point of <c>IWiredRoomHost</c>. The wired leaves have always been testable — there are
/// 33 files of them — but the parts that decide <em>which</em> leaves run, in what order, and whether
/// they run at all could only be exercised by building most of a room. Now they take a view and a
/// diagnostics sink, and this is both.
/// </remarks>
internal sealed class FakeWiredRoomHost
    : IWiredRoomHost,
        IWiredRoomView,
        IWiredDiagnostics,
        IWiredLimits
{
    private readonly Dictionary<RoomObjectId, IRoomItem> _items = [];
    private readonly Dictionary<int, List<IRoomFloorItem>> _tiles = [];

    public IWiredRoomView View => this;

    public IWiredDiagnostics Diagnostics => this;

    public List<IWiredVariable> Internal { get; } = [];

    public IReadOnlyList<IWiredVariable> InternalVariables() => Internal;

    /// <summary>Every reason a chain stopped, in order, so a test can assert on the counter the
    /// engine is supposed to bump rather than on a log line.</summary>
    public List<string> StopReasons { get; } = [];

    public List<RoomWiredLogEntry> RoomLog { get; } = [];

    public List<(string ErrorName, string Category)> Errors { get; } = [];

    // --- the room ------------------------------------------------------------------------------

    public RoomId RoomId { get; set; } = new(1);

    public long Now { get; set; }

    public long NowMs() => Now;

    public long AdvanceBoundaryPast(long now, int periodMs) => now + periodMs;

    public long NextWiredBoundaryMs { get; set; }

    public WiredVariableHash AllVariablesHash { get; set; } = new(0);

    public IWiredLimits Limits => this;

    public int MaxCallChainDepth { get; set; } = 8;

    public int WiredTickMs { get; set; } = 50;

    public int MaxQueuedEvents { get; set; } = 512;

    public int MaxEventsPerTick { get; set; } = 64;

    public int MaxScheduledPerTick { get; set; } = 64;

    public int FlashDurationMs { get; set; } = 500;

    public int TileCount { get; set; } = 100;

    public bool TryGetItem(RoomObjectId objectId, [NotNullWhen(true)] out IRoomItem? item) =>
        _items.TryGetValue(objectId, out item);

    public bool HasItem(RoomObjectId objectId) => _items.ContainsKey(objectId);

    public IReadOnlyList<IRoomItem> AllItems() => [.. _items.Values];

    public IReadOnlyList<int> AllItemIds() => [.. _items.Keys.Select(k => k.Value)];

    public List<int> AvatarPlayerIds { get; } = [];

    public IReadOnlyList<int> AllAvatarPlayerIds() => AvatarPlayerIds;

    public IReadOnlyList<IRoomFloorItem> EnumerateTileFloorStack(int tileIdx) =>
        tileIdx >= 0 && _tiles.TryGetValue(tileIdx, out List<IRoomFloorItem>? stack)
            // Ordered by object id, exactly as the real host does: the pile's order is a room rule,
            // and a fake that skipped it would let an ordering bug pass.
            ? [.. stack.OrderBy(i => i.ObjectId.Value)]
            : [];

    public bool IsOnTile(int tileIdx, RoomObjectId objectId) =>
        _tiles.TryGetValue(tileIdx, out List<IRoomFloorItem>? stack)
        && stack.Any(i => i.ObjectId == objectId);

    public int ToIdx(int x, int y) => (y * 10) + x;

    // --- diagnostics ---------------------------------------------------------------------------

    public ILogger Logger => NullLogger.Instance;

    public void ChainStopped(string reason) => StopReasons.Add(reason);

    public void WriteRoomLog(RoomWiredLogEntry entry) => RoomLog.Add(entry);

    public void RecordError(string errorName, string category, long nowMs) =>
        Errors.Add((errorName, category));

    // --- the wired limits the boxes read -------------------------------------------------------

    public int WiredSelectorMaxAreaSize { get; set; } = 100;

    public int WiredSelectedItemsLimit { get; set; } = 20;

    public int WiredNeighborhoodRadius { get; set; } = 5;

    public int WiredMaxIntParams { get; set; } = 16;

    public bool WiredAllowWallFurni { get; set; } = true;

    // --- building the room ---------------------------------------------------------------------

    /// <summary>Puts a floor item in the room, and on a tile if one is named.</summary>
    public FakeWiredRoomHost With(IRoomFloorItem item, int? tileIdx = null)
    {
        _items[item.ObjectId] = item;

        if (tileIdx is int idx)
        {
            if (!_tiles.TryGetValue(idx, out List<IRoomFloorItem>? stack))
            {
                stack = [];
                _tiles[idx] = stack;
            }

            stack.Add(item);
        }

        return this;
    }

    /// <summary>Takes an item out of the room without taking it off its tile — a stale registry
    /// entry, which is what the engine's "reindex next tick" branches exist for.</summary>
    public void RemoveItemOnly(RoomObjectId objectId) => _items.Remove(objectId);
}
