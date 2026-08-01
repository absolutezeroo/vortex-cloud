using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Action;
using Vortex.Primitives.Players;

namespace Vortex.Rooms.Grains;

public sealed partial class RoomGrain
{
    public Task<bool> KickUserAsync(
        ActionContext actorCtx,
        PlayerId targetPlayerId,
        CancellationToken ct
    ) => ModerationSystem.KickUserAsync(actorCtx, targetPlayerId, ct);

    /// <summary>Kicks a user without a human actor — for wired / system-driven kicks (the
    /// <c>wf_act_kick_user</c> action). Called directly on the grain from inside its own turn, so it is
    /// not a re-entrant grain-reference call.</summary>
    public Task<bool> KickUserFromWiredAsync(PlayerId targetPlayerId, CancellationToken ct) =>
        ModerationSystem.KickUserFromWiredAsync(targetPlayerId, ct);

    public Task<bool> MuteUserAsync(
        ActionContext actorCtx,
        PlayerId targetPlayerId,
        int durationSeconds,
        CancellationToken ct
    ) => ModerationSystem.MuteUserAsync(actorCtx, targetPlayerId, durationSeconds, ct);

    public Task<bool> BanUserAsync(
        ActionContext actorCtx,
        PlayerId targetPlayerId,
        int durationSeconds,
        CancellationToken ct
    ) => ModerationSystem.BanUserAsync(actorCtx, targetPlayerId, durationSeconds, ct);

    public Task<bool> UnmuteUserAsync(
        ActionContext actorCtx,
        PlayerId targetPlayerId,
        CancellationToken ct
    ) => ModerationSystem.UnmuteUserAsync(actorCtx, targetPlayerId, ct);

    public Task<bool> UnbanUserAsync(
        ActionContext actorCtx,
        PlayerId targetPlayerId,
        CancellationToken ct
    ) => ModerationSystem.UnbanUserAsync(actorCtx, targetPlayerId, ct);
}
