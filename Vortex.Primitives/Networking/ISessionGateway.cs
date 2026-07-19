using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Orleans.Observers;
using Vortex.Primitives.Players;

namespace Vortex.Primitives.Networking;

public interface ISessionGateway
{
    public ISessionContext? GetSession(SessionKey key);
    public ISessionContextObserver? GetSessionObserver(SessionKey key);
    public PlayerId GetPlayerId(SessionKey key);
    public int GetActiveSessionCount();
    public IReadOnlyCollection<PlayerId> GetOnlinePlayerIds();
    public Task AddSessionAsync(SessionKey key, ISessionContext ctx);
    public Task RemoveSessionAsync(SessionKey key, CancellationToken ct);
    public Task AddSessionToPlayerAsync(
        SessionKey key,
        PlayerId playerId,
        CancellationToken ct = default
    );
    public Task RemoveSessionFromPlayerAsync(PlayerId playerId, CancellationToken ct);
}
