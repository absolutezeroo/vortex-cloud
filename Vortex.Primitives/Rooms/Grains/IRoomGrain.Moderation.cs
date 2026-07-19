using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Action;
using Vortex.Primitives.Players;

namespace Vortex.Primitives.Rooms.Grains;

public partial interface IRoomGrain
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
}
