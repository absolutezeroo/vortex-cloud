using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Vortex.Primitives.Bots;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Furniture.Wall;
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
        IWiredRoomActions,
        IWiredLimits
{
    private readonly Dictionary<RoomObjectId, IRoomItem> _items = [];
    private readonly Dictionary<int, List<IRoomFloorItem>> _tiles = [];

    public IWiredRoomView View => this;

    public IWiredDiagnostics Diagnostics => this;

    public IWiredRoomActions Actions => this;

    public List<IWiredVariable> Internal { get; } = [];

    public IReadOnlyList<IWiredVariable> InternalVariables() => Internal;

    /// <summary>Every reason a chain stopped, in order, so a test can assert on the counter the
    /// engine is supposed to bump rather than on a log line.</summary>
    public List<string> StopReasons { get; } = [];

    /// <summary>What became of each room event, in order.</summary>
    public List<string> EventOutcomes { get; } = [];

    /// <summary>How many times the trigger index was rebuilt.</summary>
    public int IndexRebuilds { get; private set; }

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

    public void EventOutcome(string outcome) => EventOutcomes.Add(outcome);

    public void IndexRebuilt() => IndexRebuilds++;

    public void WriteRoomLog(RoomWiredLogEntry entry) => RoomLog.Add(entry);

    public void RecordError(string errorName, string category, long nowMs) =>
        Errors.Add((errorName, category));

    // --- what an effect did to the room --------------------------------------------------------
    //
    // Recorded rather than performed. An effect's whole observable behaviour is the calls it makes
    // here, so a test asserts on these lists instead of trying to read a room that does not exist.

    public List<(RoomObjectId Item, int TileIdx)> FloorItemMoves { get; } = [];

    public List<(RoomObjectId Item, int X, int Y)> WallItemMoves { get; } = [];

    /// <summary>Keyed by room object id: an avatar's identity inside the room is its object id.</summary>
    public List<(int ObjectId, int TileIdx)> AvatarRolls { get; } = [];

    public List<int> WalksCancelled { get; } = [];

    public List<(int ObjectId, int X, int Y)> AvatarWalks { get; } = [];

    public List<(int BotId, string What)> BotCommands { get; } = [];

    public List<IComposer> RoomComposers { get; } = [];

    public List<(int PlayerId, int HandItemId)> HandItemsGiven { get; } = [];

    /// <summary>Bots the room knows about, by name. Anything else is "somebody picked it up".</summary>
    public Dictionary<string, BotSnapshot> Bots { get; } = [];

    /// <summary>Avatars in the room, by player id.</summary>
    public Dictionary<int, IRoomAvatar> Avatars { get; } = [];

    /// <summary>The room is TileCount tiles laid out ten to a row, matching ToIdx.</summary>
    public bool InBounds(int tileIdx) => tileIdx >= 0 && tileIdx < TileCount;

    public (int X, int Y) GetTileXY(int tileIdx) => (tileIdx % 10, tileIdx / 10);

    public Altitude TileHeight(int tileIdx) => Altitude.FromInt(0);

    public bool MoveFloorItem(IRoomFloorItem item, int tileIdx, Altitude? z, Rotation? rotation)
    {
        FloorItemMoves.Add((item.ObjectId, tileIdx));

        return true;
    }

    public bool MoveWallItem(
        IRoomWallItem item,
        int x,
        int y,
        Altitude z,
        Rotation rotation,
        int wallOffset
    )
    {
        WallItemMoves.Add((item.ObjectId, x, y));

        return true;
    }

    public bool RollAvatar(IRoomAvatar avatar, int tileIdx, Altitude z)
    {
        AvatarRolls.Add((avatar.ObjectId.Value, tileIdx));

        return true;
    }

    public void CancelWalk(IRoomAvatar avatar) => WalksCancelled.Add(avatar.ObjectId.Value);

    public Task WalkAvatarToAsync(IRoomAvatar avatar, int x, int y, CancellationToken ct)
    {
        AvatarWalks.Add((avatar.ObjectId.Value, x, y));

        return Task.CompletedTask;
    }

    public bool TryGetAvatar(PlayerId playerId, [NotNullWhen(true)] out IRoomAvatar? avatar) =>
        Avatars.TryGetValue(playerId.Value, out avatar);

    public Task<BotSnapshot?> FindBotByNameAsync(string botName, CancellationToken ct) =>
        Task.FromResult(Bots.TryGetValue(botName, out BotSnapshot? bot) ? bot : null);

    public Task BotSayAsync(
        int botId,
        string text,
        WiredBotChatType chatType,
        PlayerId? whisperTo,
        CancellationToken ct
    ) => RecordAsync(botId, $"say:{text}");

    public Task BotTeleportAsync(int botId, int x, int y, CancellationToken ct) =>
        RecordAsync(botId, $"teleport:{x},{y}");

    public Task BotWalkToAsync(int botId, int x, int y, CancellationToken ct) =>
        RecordAsync(botId, $"walk:{x},{y}");

    public Task BotSetFollowTargetAsync(int botId, PlayerId? target, CancellationToken ct) =>
        RecordAsync(
            botId,
            $"follow:{target?.Value.ToString(CultureInfo.InvariantCulture) ?? "none"}"
        );

    public Task BotSetFigureAsync(int botId, string figure, CancellationToken ct) =>
        RecordAsync(botId, $"figure:{figure}");

    public Task SendComposerToRoomAsync(IComposer composer)
    {
        RoomComposers.Add(composer);

        return Task.CompletedTask;
    }

    public bool GiveHandItem(PlayerId playerId, int handItemId)
    {
        HandItemsGiven.Add((playerId.Value, handItemId));

        return true;
    }

    private Task RecordAsync(int botId, string what)
    {
        BotCommands.Add((botId, what));

        return Task.CompletedTask;
    }

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

    /// <summary>Drags a box onto another tile, the way a player would. It is still in the room.</summary>
    public void MoveToTile(int objectId, int tileIdx)
    {
        RoomObjectId id = new(objectId);

        foreach (List<IRoomFloorItem> stack in _tiles.Values)
        {
            stack.RemoveAll(i => i.ObjectId == id);
        }

        if (_items.TryGetValue(id, out IRoomItem? item) && item is IRoomFloorItem floor)
        {
            if (!_tiles.TryGetValue(tileIdx, out List<IRoomFloorItem>? target))
            {
                target = [];
                _tiles[tileIdx] = target;
            }

            target.Add(floor);
        }
    }

    /// <summary>Picks a box up: gone from the room and off every tile.</summary>
    public void RemoveCompletely(int objectId)
    {
        RoomObjectId id = new(objectId);

        _items.Remove(id);

        foreach (List<IRoomFloorItem> stack in _tiles.Values)
        {
            stack.RemoveAll(i => i.ObjectId == id);
        }
    }
}
