using System;
using Microsoft.Extensions.Options;
using Vortex.Observability.Configuration;
using Vortex.Primitives.Authentication;

namespace Vortex.Dashboard.API.Security;

/// <summary>
///     Authenticated dashboard sessions. Session ids are 256-bit cryptographically random opaque
///     tokens delivered as an HttpOnly cookie; the store holds only the backing account id and the
///     operator's email for audit attribution (capabilities are re-resolved per request so role
///     changes take effect immediately). Sessions are cleared on restart, which is acceptable for an
///     operator dashboard and avoids any persistent token.
///     <para>
///     The mechanics -- token minting, expiry, pruning, revocation -- are
///     <see cref="AccountSessionStore{TState}" />, shared with the player-facing web API so the two
///     cannot drift apart again.
///     </para>
/// </summary>
internal sealed class DashboardSessionStore : IAccountSessionRevoker
{
    public string SessionKind => "dashboard";

    private readonly AccountSessionStore<string> _sessions;

    public DashboardSessionStore(IOptions<ObservabilityConfig> options)
    {
        int minutes = Math.Max(5, options.Value.DashboardSessionLifetimeMinutes);
        _sessions = new AccountSessionStore<string>(TimeSpan.FromMinutes(minutes));
    }

    public int LifetimeSeconds => _sessions.LifetimeSeconds;

    public int Count => _sessions.Count;

    public string Create(int accountId, string email) => _sessions.Create(accountId, email);

    public (int AccountId, string Email)? Resolve(string? sessionId) =>
        _sessions.Resolve(sessionId);

    public void Remove(string? sessionId) => _sessions.Remove(sessionId);

    /// <summary>
    ///     Revokes every session of an account. Capability changes already take effect on the next
    ///     request, but a credential change does not: the cookie is what proves the operator, and it
    ///     keeps proving it until this is called.
    /// </summary>
    public int RemoveAllForAccount(int accountId) => _sessions.RemoveAllForAccount(accountId);
}
