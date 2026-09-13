using System;
using Microsoft.Extensions.Options;
using Vortex.Primitives.Authentication;
using Vortex.WebApi.Configuration;

namespace Vortex.WebApi.Session;

/// <summary>
/// What a web session remembers besides the account it belongs to.
/// </summary>
/// <param name="SelectedPlayer">The avatar the visitor picked, or none yet.</param>
/// <param name="Trusted">
/// Whether this session has cleared the account's security questions. True when the account has
/// none, and when the sign-in came from a place already answered from; false until the visitor
/// answers, which is what habbo.com's <c>safety-lock-modal</c> asks for. It lives on the SESSION and
/// not on the account because it is a property of this browser on this visit: two sessions of the
/// same account can legitimately disagree, and that is the whole point of a challenge.
/// </param>
public readonly record struct WebSessionState(int? SelectedPlayer, bool Trusted);

/// <summary>
/// Authenticated web sessions, keyed by the cookie id, remembering which avatar the visitor picked
/// and whether they have cleared the account's security questions. Cleared on server restart.
///
/// <para>
/// The mechanics are <see cref="AccountSessionStore{TState}" />, shared with the dashboard. They used
/// to be a second implementation here, and it had drifted: a GUID rather than 256 cryptographic
/// bits, a day hard-coded rather than configured, expired entries dropped only if someone happened
/// to ask, no way to revoke an account's sessions at all, and a selected-avatar read that answered
/// for sessions that had already expired.
/// </para>
/// </summary>
public sealed class WebApiSessionStore : IAccountSessionRevoker
{
    public string SessionKind => "web";

    private readonly AccountSessionStore<WebSessionState> _sessions;

    public WebApiSessionStore(IOptions<WebApiConfig> options)
    {
        int hours = Math.Max(1, options.Value.SessionLifetimeHours);
        _sessions = new AccountSessionStore<WebSessionState>(TimeSpan.FromHours(hours));
    }

    public int LifetimeSeconds => _sessions.LifetimeSeconds;

    /// <param name="trusted">
    /// Decided by the caller at sign-in, the only place that has an address and a user agent to
    /// build a fingerprint from. It defaults to true so an account with no security questions — and
    /// every caller that has no opinion — behaves exactly as before.
    /// </param>
    public string CreateSession(int accountId, bool trusted = true) =>
        _sessions.Create(accountId, new WebSessionState(null, trusted));

    public int? GetAccountId(string? sessionId) => _sessions.Resolve(sessionId)?.AccountId;

    public void RemoveSession(string sessionId) => _sessions.Remove(sessionId);

    /// <summary>
    /// Revokes every session of an account, for a password change or a sanction. Nothing calls it on
    /// a ban yet: a web session is resolved to an account id and never re-checked against the
    /// account's standing, so a banned visitor keeps browsing until the cookie expires.
    /// </summary>
    public int RemoveAllForAccount(int accountId) => _sessions.RemoveAllForAccount(accountId);

    public void SetSelectedPlayer(string? sessionId, int playerId) =>
        _sessions.TryUpdate(sessionId, state => state with { SelectedPlayer = playerId });

    public int? GetSelectedPlayer(string? sessionId) =>
        _sessions.Resolve(sessionId)?.State.SelectedPlayer;

    /// <summary>
    /// A session nobody can resolve is not trusted. That is the safe default and not a detail: an
    /// expired cookie answering "trusted" would walk straight past the challenge.
    /// </summary>
    public bool IsTrusted(string? sessionId) =>
        _sessions.Resolve(sessionId)?.State.Trusted ?? false;

    /// <summary>The challenge was answered correctly, for THIS session.</summary>
    public void MarkTrusted(string? sessionId) =>
        _sessions.TryUpdate(sessionId, state => state with { Trusted = true });
}
