using System;
using System.Collections.Concurrent;

namespace Vortex.WebApi.Session;

/// <summary>
/// In-memory store mapping session cookie IDs to authenticated account IDs.
/// Sessions expire after 24 hours. Cleared on server restart (dev-acceptable).
/// </summary>
public sealed class WebApiSessionStore
{
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(24);

    private readonly ConcurrentDictionary<string, SessionEntry> _sessions = new();

    public string CreateSession(int accountId)
    {
        string id = Guid.NewGuid().ToString("N");
        _sessions[id] = new SessionEntry(accountId, DateTime.UtcNow.Add(SessionLifetime));
        return id;
    }

    public int? GetAccountId(string? sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return null;
        }

        if (!_sessions.TryGetValue(sessionId, out SessionEntry? entry))
        {
            return null;
        }

        if (entry.Expires < DateTime.UtcNow)
        {
            _sessions.TryRemove(sessionId, out _);
            return null;
        }

        return entry.AccountId;
    }

    public void RemoveSession(string sessionId) => _sessions.TryRemove(sessionId, out _);

    public void SetSelectedPlayer(string? sessionId, int playerId)
    {
        if (
            !string.IsNullOrWhiteSpace(sessionId)
            && _sessions.TryGetValue(sessionId, out SessionEntry? e)
        )
        {
            _sessions[sessionId] = e with { SelectedPlayerId = playerId };
        }
    }

    public int? GetSelectedPlayer(string? sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return null;
        }

        return _sessions.TryGetValue(sessionId, out SessionEntry? e) ? e.SelectedPlayerId : null;
    }

    private record SessionEntry(int AccountId, DateTime Expires, int? SelectedPlayerId = null);
}
