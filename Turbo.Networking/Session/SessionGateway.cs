using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Turbo.Primitives.Events;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Orleans.Observers;
using Turbo.Primitives.Players;
using Turbo.Primitives.Players.Grains;

namespace Turbo.Networking.Session;

public sealed class SessionGateway(
    IGrainFactory grainFactory,
    IEventPublisher events,
    ILogger<SessionGateway> logger
) : ISessionGateway
{
    private readonly IGrainFactory _grainFactory = grainFactory;
    private readonly IEventPublisher _events = events;
    private readonly ILogger<SessionGateway> _logger = logger;

    private readonly ConcurrentDictionary<SessionKey, ISessionContext> _sessions = new();
    private readonly ConcurrentDictionary<SessionKey, ObserverEntry> _sessionObservers = new();
    private readonly ConcurrentDictionary<SessionKey, PlayerId> _sessionToPlayer = new();
    private readonly ConcurrentDictionary<PlayerId, SessionKey> _playerToSession = new();
    private readonly ConcurrentDictionary<PlayerId, DateTime> _playerConnectedAt = new();

    private sealed record ObserverEntry(SessionContextObserver Impl, ISessionContextObserver Ref);

    public ISessionContext? GetSession(SessionKey key) =>
        _sessions.TryGetValue(key, out ISessionContext? ctx) ? ctx : null;

    public ISessionContextObserver? GetSessionObserver(SessionKey key) =>
        _sessionObservers.TryGetValue(key, out ObserverEntry? observer) ? observer.Ref : null;

    public PlayerId GetPlayerId(SessionKey key) =>
        _sessionToPlayer.TryGetValue(key, out PlayerId playerId) ? playerId : -1;

    public int GetActiveSessionCount() => _sessions.Count;

    public IReadOnlyCollection<PlayerId> GetOnlinePlayerIds() => _playerToSession.Keys.ToArray();

    public Task AddSessionAsync(SessionKey key, ISessionContext ctx)
    {
        _sessions[key] = ctx;

        _sessionObservers.AddOrUpdate(
            key,
            _ =>
            {
                SessionContextObserver impl = new SessionContextObserver(key, this);
                ISessionContextObserver objRef =
                    _grainFactory.CreateObjectReference<ISessionContextObserver>(impl);

                return new ObserverEntry(impl, objRef);
            },
            (_, existing) => existing
        );

        return Task.CompletedTask;
    }

    public async Task RemoveSessionAsync(SessionKey key, CancellationToken ct)
    {
        if (_sessionToPlayer.TryGetValue(key, out PlayerId playerId) && playerId > 0)
        {
            await RemovePlayerSessionAsync(playerId, key, closeTransport: false, ct)
                .ConfigureAwait(false);
        }

        if (_sessionObservers.TryRemove(key, out ObserverEntry? observer))
        {
            try
            {
                _grainFactory.DeleteObjectReference<ISessionContextObserver>(observer.Ref);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(
                    ex,
                    "Failed to delete session observer for session {SessionKey}",
                    key
                );
            }
        }

        _sessions.TryRemove(key, out _);
    }

    public async Task AddSessionToPlayerAsync(SessionKey key, PlayerId playerId)
    {
        ISessionContextObserver? observer = GetSessionObserver(key);

        if (observer is null)
        {
            return;
        }

        IPlayerPresenceGrain playerPresence = _grainFactory.GetPlayerPresenceGrain(playerId);

        if (
            _sessionToPlayer.TryGetValue(key, out PlayerId currentPlayerId)
            && currentPlayerId > 0
            && currentPlayerId != playerId
        )
        {
            await RemovePlayerSessionAsync(
                    currentPlayerId,
                    key,
                    closeTransport: false,
                    CancellationToken.None
                )
                .ConfigureAwait(false);
        }

        if (
            _playerToSession.TryGetValue(playerId, out SessionKey existingSessionKey)
            && existingSessionKey != key
        )
        {
            await RemovePlayerSessionAsync(
                    playerId,
                    existingSessionKey,
                    closeTransport: true,
                    CancellationToken.None
                )
                .ConfigureAwait(false);
        }

        _sessionToPlayer[key] = playerId;
        _playerToSession[playerId] = key;
        DateTime connectedAt = DateTime.UtcNow;
        _playerConnectedAt[playerId] = connectedAt;

        await playerPresence.RegisterSessionObserverAsync(observer).ConfigureAwait(false);
        await PublishConnectedEventSafelyAsync(playerId, connectedAt).ConfigureAwait(false);
    }

    private async Task PublishConnectedEventSafelyAsync(PlayerId playerId, DateTime connectedAt)
    {
        try
        {
            await _events
                .PublishAsync(new PlayerConnectedEvent(playerId, connectedAt))
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to publish player connected lifecycle event for {PlayerId}",
                playerId
            );
        }
    }

    public async Task RemoveSessionFromPlayerAsync(PlayerId playerId, CancellationToken ct)
    {
        if (!_playerToSession.TryGetValue(playerId, out SessionKey sessionKey))
        {
            return;
        }

        await RemovePlayerSessionAsync(playerId, sessionKey, closeTransport: true, ct)
            .ConfigureAwait(false);
    }

    private async Task RemovePlayerSessionAsync(
        PlayerId playerId,
        SessionKey sessionKey,
        bool closeTransport,
        CancellationToken ct
    )
    {
        bool removedActiveMapping = TryRemovePair(_playerToSession, playerId, sessionKey);
        TryRemovePair(_sessionToPlayer, sessionKey, playerId);

        if (!removedActiveMapping)
        {
            return;
        }

        IPlayerPresenceGrain playerPresence = _grainFactory.GetPlayerPresenceGrain(playerId);

        try
        {
            await playerPresence.UnregisterSessionObserverAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to unregister session observer for player {PlayerId}",
                playerId
            );
        }

        if (_playerConnectedAt.TryRemove(playerId, out DateTime connectedAt))
        {
            await PublishDisconnectedEventSafelyAsync(playerId, connectedAt).ConfigureAwait(false);
        }
        else
        {
            _logger.LogWarning(
                "Player {PlayerId} disconnected without a recorded connect timestamp; skipping disconnect duration.",
                playerId
            );
        }

        if (closeTransport && _sessions.TryGetValue(sessionKey, out ISessionContext? session))
        {
            await CloseSessionTransportSafelyAsync(sessionKey, session).ConfigureAwait(false);
        }
    }

    private async Task CloseSessionTransportSafelyAsync(
        SessionKey sessionKey,
        ISessionContext session
    )
    {
        try
        {
            await session.CloseSessionAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to close session transport for {SessionKey}", sessionKey);
        }
    }

    private async Task PublishDisconnectedEventSafelyAsync(PlayerId playerId, DateTime connectedAt)
    {
        DateTime disconnectedAt = DateTime.UtcNow;
        TimeSpan duration = disconnectedAt - connectedAt;

        try
        {
            await _events
                .PublishAsync(
                    new PlayerDisconnectedEvent(
                        playerId,
                        connectedAt,
                        disconnectedAt,
                        Math.Max(0, (long)duration.TotalSeconds)
                    )
                )
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to publish player disconnected lifecycle event for {PlayerId}",
                playerId
            );
        }
    }

    private static bool TryRemovePair<TKey, TValue>(
        ConcurrentDictionary<TKey, TValue> dictionary,
        TKey key,
        TValue value
    )
        where TKey : notnull =>
        ((ICollection<KeyValuePair<TKey, TValue>>)dictionary).Remove(
            new KeyValuePair<TKey, TValue>(key, value)
        );
}
