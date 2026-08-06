using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Action;
using Vortex.Primitives.Players;

namespace Vortex.Primitives.Rooms.Grains;

/// <summary>In-room moderation actions taken against a present player.</summary>
[Alias("Vortex.Primitives.Rooms.Grains.IRoomModeration")]
public interface IRoomModeration : IGrainWithIntegerKey
{
    public Task<bool> KickUserAsync(
        ActionContext actorCtx,
        PlayerId targetPlayerId,
        CancellationToken ct
    );
    public Task<bool> MuteUserAsync(
        ActionContext actorCtx,
        PlayerId targetPlayerId,
        int durationSeconds,
        CancellationToken ct
    );
    public Task<bool> BanUserAsync(
        ActionContext actorCtx,
        PlayerId targetPlayerId,
        int durationSeconds,
        CancellationToken ct
    );
    public Task<bool> UnmuteUserAsync(
        ActionContext actorCtx,
        PlayerId targetPlayerId,
        CancellationToken ct
    );
    public Task<bool> UnbanUserAsync(
        ActionContext actorCtx,
        PlayerId targetPlayerId,
        CancellationToken ct
    );

    /// <summary>
    /// The three room-tool checkboxes a staff member can apply to a room they do not own, in one
    /// pass. Unlike everything else on this facet the actor is not required to be present in the
    /// room, and no in-room controller level is consulted — <b>authorization is the caller's
    /// responsibility</b> (<c>Capabilities.Room.ModerateAny</c>), the same contract as
    /// <see cref="IRoomSettings.SetStaffPickAsync"/>.
    /// </summary>
    /// <param name="unlockDoor">Force the door back to open, undoing a lock used to trap visitors.</param>
    /// <param name="resetNameAndDescription">Replace an offensive name and description with a neutral placeholder.</param>
    /// <param name="kickUsers">Remove everyone currently in the room.</param>
    /// <returns>Whether anything was actually applied.</returns>
    public Task<bool> ApplyStaffRoomActionsAsync(
        PlayerId actorPlayerId,
        bool unlockDoor,
        bool resetNameAndDescription,
        bool kickUsers,
        CancellationToken ct
    );
}
