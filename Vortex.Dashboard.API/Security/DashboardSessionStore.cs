using System;
using System.Collections.Concurrent;
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
