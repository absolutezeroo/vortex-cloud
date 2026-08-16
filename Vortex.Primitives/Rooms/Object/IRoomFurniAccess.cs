using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Action;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Primitives.Rooms.Wired.Variable;

namespace Vortex.Primitives.Rooms.Object;

/// <summary>
/// Placement validation and the wired engine's room-level knobs, as furniture logic reaches them.
/// Not a grain contract. Only <see cref="ValidateFloorItemPlacementAsync"/> is asynchronous, because
/// it is the only one that is asynchronous underneath.
/// </summary>
public interface IRoomFurniAccess
{
    /// <summary>Whether this floor item may sit at this position and rotation.</summary>
    Task<bool> ValidateFloorItemPlacementAsync(
        ActionContext ctx,
        RoomObjectId itemId,
        int x,
        int y,
        Rotation rot
    );

    /// <summary>The live wired variable behind an id, or null when it no longer exists.</summary>
    IWiredVariable? GetVariableById(WiredVariableId id);

    /// <summary>Queues the brief visual flash a wired box shows when it fires.</summary>
    void ScheduleFlashRevert(RoomObjectId objectId);

    /// <summary>Re-anchors every wired timer in the room to the current room clock.</summary>
    void ResetTimers();

    /// <summary>Hash over every wired variable in the room, stamped into variable snapshots so the
    /// client can tell a stale page from a current one.</summary>
    WiredVariableHash AllVariablesHash { get; }

    /// <summary>The store holding this key's value, room-wide. False when nothing owns it.</summary>
    bool TryGetVariableStore(WiredVariableKey key, out IWiredKeyValueStore? store);

    /// <summary>Kicks a player on a wired action's behalf. Distinct from ordinary moderation: there
    /// is no actor to authorize, the room's own wiring is doing it.</summary>
    Task<bool> KickUserFromWiredAsync(PlayerId targetPlayerId, CancellationToken ct);

    /// <summary>
    /// Loads a guild's member roster into the room so <see cref="IsGuildMember"/> can answer without
    /// awaiting. Cheap to call repeatedly: the roster is cached for a short while, and the room's own
    /// guild is served from the roster the security module already keeps live.
    /// </summary>
    Task EnsureGuildRosterAsync(int groupId, CancellationToken ct);

    /// <summary>Whether the player belongs to the guild, from the roster
    /// <see cref="EnsureGuildRosterAsync"/> loaded. False when no roster was loaded — a wired box
    /// that never prepared must not pass.</summary>
    bool IsGuildMember(int groupId, PlayerId player);

    /// <summary>Loads the badges a player currently wears into the room, for the same reason
    /// <see cref="EnsureGuildRosterAsync"/> exists.</summary>
    Task EnsureWornBadgesAsync(PlayerId player, CancellationToken ct);

    /// <summary>Whether the player wears this badge code, from what
    /// <see cref="EnsureWornBadgesAsync"/> loaded. Worn means occupying one of the five profile
    /// slots, not merely owned.</summary>
    bool IsWearingBadge(PlayerId player, string badgeCode);

    /// <summary>
    /// Runs the piles under these furni, bypassing their own triggers and conditions — the
    /// "execute stacks" action. The calling box's tile is passed so a pile cannot execute itself,
    /// and the caller's selection carries into the called piles.
    /// </summary>
    /// <returns>How many piles were executed.</returns>
    Task<int> ExecuteWiredStacksAtAsync(
        int callerTileIdx,
        IReadOnlyCollection<int> targetFurniIds,
        IWiredSelectionSet inheritedSelection,
        CancellationToken ct
    );
}
