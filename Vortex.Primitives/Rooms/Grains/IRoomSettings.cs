using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Navigator.Enums;
using Vortex.Primitives.Orleans.Snapshots.Room;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.Primitives.Rooms.Grains;

/// <summary>The room's own configuration: settings, rights lists, tags, rating and deletion.</summary>
[Alias("Vortex.Primitives.Rooms.Grains.IRoomSettings")]
public interface IRoomSettings : IGrainWithIntegerKey
{
    Task<RoomSnapshot?> GetRoomSettingsAsync(PlayerId actor, CancellationToken ct);

    Task<bool> UpdateRoomSettingsAsync(
        PlayerId actor,
        RoomSettingsUpdate update,
        CancellationToken ct
    );

    /// <summary>
    /// Applies a floor-plan editor save: a new model for the room, and the door, thickness and wall
    /// height that came with it.
    ///
    /// Unlike every other setting this one changes the room's *dimensions*, so it rebuilds the tile
    /// map, re-seats the furniture on it and re-broadcasts the height map to everyone inside.
    /// </summary>
    Task<bool> UpdateFloorPlanAsync(PlayerId actor, FloorPlanUpdate update, CancellationToken ct);

    Task<bool> DeleteRoomAsync(PlayerId actor, CancellationToken ct);

    Task<ImmutableArray<RoomControllerSnapshot>> GetControllersAsync(CancellationToken ct);

    Task<ImmutableArray<RoomControllerSnapshot>> GetBannedUsersAsync(CancellationToken ct);

    Task<bool> UpdateCategoryAndTradeAsync(
        PlayerId actor,
        int categoryId,
        RoomTradeModeType tradeType,
        CancellationToken ct
    );

    Task AssignRightsAsync(PlayerId actor, PlayerId target, CancellationToken ct);

    Task RemoveRightsAsync(PlayerId actor, ImmutableArray<PlayerId> targets, CancellationToken ct);

    Task RemoveAllRightsAsync(PlayerId actor, CancellationToken ct);

    Task RemoveOwnRightsAsync(PlayerId actor, CancellationToken ct);

    /// <summary>Owner-only. Max two tags (SetRoomSessionTagsMessage never sends more); null/blank
    /// clears a slot.</summary>
    Task<bool> SetRoomTagsAsync(PlayerId actor, string? tag1, string? tag2, CancellationToken ct);

    /// <summary>One vote per player per room, enforced by the caller via RoomRatingEntity. Owner
    /// cannot rate their own room. Returns false if the vote was rejected.</summary>
    Task<bool> RateRoomAsync(PlayerId actor, int points, CancellationToken ct);

    /// <summary>Staff-only (Capabilities.Navigator.StaffPick) -- authorization is the caller's
    /// responsibility, this just applies the flag.</summary>
    Task SetStaffPickAsync(bool staffPick, CancellationToken ct);
}
