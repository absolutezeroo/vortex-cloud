using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Vortex.Primitives.Rooms;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Rooms.Wired.Logs;

namespace Vortex.Rooms.Wired.Engine;

/// <summary>
/// Everything the wired engine needs from the room it runs in, and nothing else.
/// </summary>
/// <remarks>
/// <para>
/// The engine used to reach into <c>RoomGrain</c> directly — <c>_roomGrain._state.ItemsById</c>,
/// <c>_roomGrain.MapModule</c>, <c>_roomGrain._logger</c> — which meant the only way to exercise the
/// pipeline was to build most of a room. The leaves are heavily tested; the orchestrator that decides
/// which of them run, in what order, and whether they run at all, was not testable at all.
/// </para>
/// <para>
/// No mutable collection crosses this boundary. <see cref="IWiredRoomView.EnumerateTileFloorStack"/>
/// hands back a sequence materialised inside the turn, <see cref="IWiredRoomView.TryGetItem"/> hands
/// back one object — never the map itself. A component holding a reference to the room's own
/// dictionary would be able to mutate room state from outside the ordering the grain guarantees,
/// which is the whole thing the actor model is here to prevent.
/// </para>
/// </remarks>
internal interface IWiredRoomHost
{
    IWiredRoomView View { get; }

    IWiredDiagnostics Diagnostics { get; }

    /// <summary>What a wired effect is allowed to do to the room.</summary>
    IWiredRoomActions Actions { get; }

    /// <summary>
    /// The variables the room itself exposes to wired boxes — the clock, the population, the
    /// scoreboards — rebuilt each time because they are computed from live state rather than stored.
    /// </summary>
    /// <remarks>
    /// The provider that builds these takes the whole grain, which is exactly what the engine must
    /// not hold. Asking the host for the finished list keeps that dependency on the room side of the
    /// boundary instead of dragging <c>IRoomGrain</c> across it.
    /// </remarks>
    IReadOnlyList<IWiredVariable> InternalVariables();
}

/// <summary>The room as the engine reads it: identity, clocks, items, tiles, avatars, budgets.</summary>
internal interface IWiredRoomView
{
    RoomId RoomId { get; }

    /// <summary>The room clock, in milliseconds. Monotonic, and the only clock the engine may read —
    /// wall time would make a delayed effect fire early after a clock change.</summary>
    long NowMs();

    /// <summary>The next multiple of <paramref name="periodMs"/> strictly after <paramref name="now"/>,
    /// so a late tick does not compound into a drifting schedule.</summary>
    long AdvanceBoundaryPast(long now, int periodMs);

    /// <summary>When the wired step is next due. Owned by the room, read and written by the engine.</summary>
    long NextWiredBoundaryMs { get; set; }

    /// <summary>The hash of every permanent variable, so a reload can tell whether anything moved.</summary>
    WiredVariableHash AllVariablesHash { get; set; }

    /// <summary>The tuning knobs, already resolved from configuration.</summary>
    IWiredLimits Limits { get; }

    /// <summary>How deep an "execute stacks" chain may go (RFW-101: configured, not a const).</summary>
    int MaxCallChainDepth { get; }

    int WiredTickMs { get; }

    int MaxQueuedEvents { get; }

    int MaxEventsPerTick { get; }

    int MaxScheduledPerTick { get; }

    int FlashDurationMs { get; }

    bool TryGetItem(RoomObjectId objectId, [NotNullWhen(true)] out IRoomItem? item);

    bool HasItem(RoomObjectId objectId);

    /// <summary>Every item in the room, materialised. Used for index rebuilds, never on the hot path.</summary>
    IReadOnlyList<IRoomItem> AllItems();

    /// <summary>Every item id in the room, materialised.</summary>
    IReadOnlyList<int> AllItemIds();

    /// <summary>Every avatar's player id, materialised.</summary>
    IReadOnlyList<int> AllAvatarPlayerIds();

    /// <summary>How many tiles the room has, so a tile index can be bounds-checked.</summary>
    int TileCount { get; }

    /// <summary>
    /// The floor items on one tile, lowest object id first — the order a pile resolves in. Materialised
    /// inside the turn: the room's own stack is never handed out.
    /// </summary>
    IReadOnlyList<IRoomFloorItem> EnumerateTileFloorStack(int tileIdx);

    /// <summary>Whether an item is still on the tile its pile was resolved from.</summary>
    bool IsOnTile(int tileIdx, RoomObjectId objectId);

    int ToIdx(int x, int y);
}

/// <summary>Where the engine says what it did, and what it refused to do.</summary>
internal interface IWiredDiagnostics
{
    ILogger Logger { get; }

    /// <summary>Counts a chain that stopped short, by one of the <c>WiredStopReason</c> values.</summary>
    void ChainStopped(string reason);

    /// <summary>Counts a room event's fate, by one of the <c>WiredEventOutcome</c> values.</summary>
    void EventOutcome(string outcome);

    /// <summary>Counts a rebuild of this room's trigger index.</summary>
    void IndexRebuilt();

    /// <summary>Writes to the room's own wired log — the in-game debugging channel.</summary>
    void WriteRoomLog(RoomWiredLogEntry entry);

    /// <summary>
    /// Records that a box threw. The room keeps one counter per error name so a box failing on every
    /// tick is reported once rather than sixteen times a second; the counter itself stays inside the
    /// room, because it is mutable state the engine has no business holding a reference to.
    /// </summary>
    void RecordError(string errorName, string category, long nowMs);
}
