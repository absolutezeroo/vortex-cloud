using System;
using Microsoft.Extensions.Options;
using Vortex.Primitives.Authentication;
using Vortex.WebApi.Configuration;

namespace Vortex.WebApi.Session;

/// <summary>
/// Authenticated web sessions, keyed by the cookie id, remembering which avatar the visitor picked.
/// Cleared on server restart.
///
/// <para>
/// The mechanics are <see cref="AccountSessionStore{TState}" />, shared with the dashboard. They used
/// to be a second implementation here, and it had drifted: a GUID rather than 256 cryptographic
/// bits, a day hard-coded rather than configured, expired entries dropped only if someone happened
/// to ask, no way to revoke an account's sessions at all, and a selected-avatar read that answered
/// for sessions that had already expired.
/// </para>
/// </summary>
public sealed class WebApiSessionStore
{
    private readonly AccountSessionStore<int?> _sessions;

    public WebApiSessionStore(IOptions<WebApiConfig> options)
    {
        int hours = Math.Max(1, options.Value.SessionLifetimeHours);
        _sessions = new AccountSessionStore<int?>(TimeSpan.FromHours(hours));
    }

    public int LifetimeSeconds => _sessions.LifetimeSeconds;

    public string CreateSession(int accountId) => _sessions.Create(accountId, null);

    public int? GetAccountId(string? sessionId) => _sessions.Resolve(sessionId)?.AccountId;

    public void RemoveSession(string sessionId) => _sessions.Remove(sessionId);

    /// <summary>
    /// Revokes every session of an account, for a password change or a sanction. Nothing calls it on
    /// a ban yet: a web session is resolved to an account id and never re-checked against the
    /// account's standing, so a banned visitor keeps browsing until the cookie expires.
    /// </summary>
    public int RemoveAllForAccount(int accountId) => _sessions.RemoveAllForAccount(accountId);

    public void SetSelectedPlayer(string? sessionId, int playerId) =>
        _sessions.TryUpdate(sessionId, _ => playerId);

    public int? GetSelectedPlayer(string? sessionId) => _sessions.Resolve(sessionId)?.State;
}
