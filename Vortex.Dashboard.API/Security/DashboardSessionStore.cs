using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Vortex.Observability.Configuration;

namespace Vortex.Dashboard.API.Security;

/// <summary>
///     In-memory store of authenticated dashboard sessions. Session ids are 256-bit cryptographically
///     random opaque tokens delivered as an HttpOnly cookie; the store holds only the backing account id
///     (capabilities are re-resolved per request so role changes take effect immediately). Sessions are
///     cleared on restart, which is acceptable for an operator dashboard and avoids any persistent token.
/// </summary>
internal sealed class DashboardSessionStore
{
    /// <summary>Live sessions past which <see cref="Create" /> sweeps the expired ones first.</summary>
    private const int PRUNE_THRESHOLD = 64;

    private readonly TimeSpan _lifetime;
    private readonly ConcurrentDictionary<string, Entry> _sessions = new(StringComparer.Ordinal);

    public DashboardSessionStore(IOptions<ObservabilityConfig> options)
    {
        int minutes = Math.Max(5, options.Value.DashboardSessionLifetimeMinutes);
        _lifetime = TimeSpan.FromMinutes(minutes);
    }

    public int LifetimeSeconds => (int)_lifetime.TotalSeconds;

    public string Create(int accountId, string email)
    {
        // Entries are otherwise only dropped when re-presented (see Resolve), so an operator who never
        // comes back leaves one behind until restart. Login is the only place the store grows, so it is
        // also the only place worth sweeping -- cheaper than a timer for a handful of operators.
        if (_sessions.Count >= PRUNE_THRESHOLD)
        {
            DateTime cutoff = DateTime.UtcNow;

            foreach (KeyValuePair<string, Entry> pair in _sessions)
            {
                if (pair.Value.ExpiresAt <= cutoff)
                {
                    _sessions.TryRemove(pair.Key, out _);
                }
            }
        }

        string sessionId = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        _sessions[sessionId] = new Entry(accountId, email, DateTime.UtcNow.Add(_lifetime));
        return sessionId;
    }

    public (int AccountId, string Email)? Resolve(string? sessionId)
    {
        if (string.IsNullOrEmpty(sessionId) || !_sessions.TryGetValue(sessionId, out Entry entry))
        {
            return null;
        }

        if (entry.ExpiresAt <= DateTime.UtcNow)
        {
            _sessions.TryRemove(sessionId, out _);
            return null;
        }

        return (entry.AccountId, entry.Email);
    }

    public void Remove(string? sessionId)
    {
        if (!string.IsNullOrEmpty(sessionId))
        {
            _sessions.TryRemove(sessionId, out _);
        }
    }

    private readonly record struct Entry(int AccountId, string Email, DateTime ExpiresAt);
}
