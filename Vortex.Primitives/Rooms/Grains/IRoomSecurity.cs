using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Action;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.Primitives.Rooms.Grains;

/// <summary>Who may do what in the room: effective controller level and the guild-derived rights
/// that feed it.</summary>
[Alias("Vortex.Primitives.Rooms.Grains.IRoomSecurity")]
public interface IRoomSecurity : IGrainWithIntegerKey
{
    /// <summary>
    /// Resolves <paramref name="ctx"/>'s effective rights in this room (owner/explicit
    /// rights/staff capabilities). Callers outside the grain (e.g. the room-entry flow) must go
    /// through this instead of approximating from a raw owner-id comparison.
    /// </summary>
    public Task<RoomControllerType> GetControllerLevelAsync(
        ActionContext ctx,
        CancellationToken ct
    );

    /// <summary>
    /// Re-reads the owning guild's roster and decoration policy into live state and re-pushes the
    /// controller level of every affected player. Called by <c>GroupGrain</c> after membership or
    /// settings changes so guild-derived rights take effect in the current session rather than at the
    /// next room load. A no-op for rooms that are not a guild base.
    /// </summary>
    /// <param name="affectedPlayerIds">
    /// Players whose controller level must be re-pushed; empty means "settings changed, refresh
    /// everyone currently in the room".
    /// </param>
    public Task RefreshGroupMembershipAsync(
        IReadOnlyList<int> affectedPlayerIds,
        CancellationToken ct
    );

    /// <summary>
    /// Re-badges <paramref name="playerId"/>'s avatar after they changed their favourite guild, and
    /// tells everyone in the room about it. A no-op when the player is not in this room.
    /// </summary>
    public Task UpdateAvatarFavouriteGroupAsync(
        PlayerId playerId,
        int groupId,
        string groupName,
        CancellationToken ct
    );
}
