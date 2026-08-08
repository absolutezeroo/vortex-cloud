using System;
using System.Collections.Generic;
using Vortex.Primitives.Groups.Enums;
using Vortex.Primitives.Orleans.Snapshots.Room;
using Vortex.Primitives.Pets.Snapshots;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Snapshots.Mapping;
using Vortex.Primitives.Rooms.Wired.Variable;

namespace Vortex.Rooms.Grains;

public sealed class RoomLiveState
{
    public required RoomId RoomId { get; init; }
    public RoomSnapshot RoomSnapshot { get; set; } = default!;

    public Dictionary<RoomObjectId, IRoomItem> ItemsById { get; } = [];
    public Dictionary<RoomObjectId, IRoomAvatar> AvatarsByObjectId { get; } = [];
    public Dictionary<PlayerId, RoomObjectId> AvatarsByPlayerId { get; } = [];
    public Dictionary<int, PetSnapshot> PetsById { get; } = [];
    public Dictionary<PlayerId, string> OwnerNamesById { get; } = [];

    public RoomModelSnapshot? Model { get; internal set; } = null;
    public Altitude[] TileHeights { get; internal set; } = [];
    public short[] TileEncodedHeights { get; internal set; } = [];
    public RoomTileFlags[] TileFlags { get; internal set; } = [];
    public RoomObjectId[] TileHighestFloorItems { get; internal set; } = [];
    public HashSet<RoomObjectId>[] TileFloorStacks { get; internal set; } = [];
    public HashSet<RoomObjectId>[] TileAvatarStacks { get; internal set; } = [];

    public HashSet<PlayerId> PlayerIdsWithRights { get; } = [];

    /// <summary>Member ranks for the guild that owns this room, keyed by player id. Empty when the
    /// room has no guild. Hydrated on room load and kept in sync by
    /// <c>RoomGrain.RefreshGroupMembershipAsync</c> — <see cref="Modules.RoomSecurityModule"/> reads
    /// it for every controller-level check, so it must never go stale while the room is active.</summary>
    public Dictionary<PlayerId, GroupMemberRank> GroupMemberRanks { get; } = [];

    /// <summary>Mirrors the owning guild's <c>admin_only_decoration</c> setting: when true, plain
    /// guild members get no build rights in the base room.</summary>
    public bool GroupAdminOnlyDecoration { get; internal set; } = false;

    public Dictionary<PlayerId, DateTime> MuteExpiresUtc { get; } = [];

    /// <summary>Players currently waiting on a doorbell ring for a locked door, keyed by the tick
    /// timestamp (ms) the ring started — swept for timeout by <c>RoomGrain.Doorbell.cs</c>.</summary>
    public Dictionary<PlayerId, long> PendingDoorbellRingersMs { get; } = [];

    /// <summary>Active trade sessions keyed by each participant's player id — both traders map to the
    /// same <see cref="RoomTradeSession"/> instance. Managed by <c>RoomGrain.Trading.cs</c>.</summary>
    internal Dictionary<PlayerId, RoomTradeSession> TradeSessionsByPlayerId { get; } = [];

    /// <summary>Mystery boxes waiting for their other half, keyed by the box's owner — the only
    /// identifier the client's cancel message carries. Managed by <c>RoomGrain.MysteryBox.cs</c>.</summary>
    internal Dictionary<PlayerId, RoomMysteryBoxSession> MysteryBoxSessionsByOwnerId { get; } = [];

    public Dictionary<string, string> RoomProperties { get; } = [];

    public HashSet<int> DirtyHeightTileIds { get; set; } = [];
    public HashSet<RoomObjectId> DirtyItemIds { get; set; } = [];
    public HashSet<RoomObjectId> DirtyFloorItemIds { get; set; } = [];
    public HashSet<RoomObjectId> DirtyWallItemIds { get; set; } = [];

    public WiredVariableHash AllVariablesHash { get; internal set; } = new WiredVariableHash(0);

    /// <summary>Aggregated wired-execution error counters for this room's Monitor tab, keyed by
    /// error type name. In-memory only (cleared on demand or room unload) — the wire shape is a
    /// count-per-error-type summary, not an event log; see <c>WiredGetErrorLogsMessageHandler</c>.</summary>
    public Dictionary<string, WiredErrorLogCounter> WiredErrorLogCounters { get; } = [];

    public bool IsMapReady { get; internal set; } = false;
    public bool IsFurniLoaded { get; internal set; } = false;
    public bool IsPetsLoaded { get; internal set; } = false;
    public bool IsTileComputationPaused { get; internal set; } = false;

    public long EpochMs { get; set; } = 0;
    public long NextAvatarBoundaryMs { get; set; } = 0;
    public long NextPetBoundaryMs { get; set; } = 0;
    public long NextBotBoundaryMs { get; set; } = 0;
    public long NextRollerBoundaryMs { get; set; } = 0;
    public long NextWiredBoundaryMs { get; set; } = 0;
}
