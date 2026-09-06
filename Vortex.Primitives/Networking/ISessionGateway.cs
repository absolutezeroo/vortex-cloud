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

    /// <summary>
    /// Whether one player is connected.
    /// </summary>
    /// <remarks>
    /// The question the dashboard's player profile asks, and it used to be answered by copying every
    /// online player id into an array and scanning it. On a full hotel that is an allocation
    /// proportional to the population, per lookup, for a boolean.
    /// </remarks>
    public bool IsOnline(PlayerId playerId);

    /// <summary>
    /// How many distinct players are connected.
    /// </summary>
    /// <remarks>
    /// Not the same as <see cref="GetActiveSessionCount"/>, which counts sockets: a player with two
    /// windows open is one player and two sessions. Separate from
    /// <see cref="GetOnlinePlayerIds"/> because three callers only ever wanted the number — the
    /// concurrent-users quest, its reward, and the connection gauge, which is read on every metrics
    /// scrape and was allocating the whole list to call <c>.Count</c> on it.
    /// </remarks>
    public int GetOnlinePlayerCount();

    /// <summary>
    /// Every connected player. Allocates a snapshot, so prefer <see cref="IsOnline"/> or
    /// <see cref="GetOnlinePlayerCount"/> when one of those answers the question.
    /// </summary>
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
