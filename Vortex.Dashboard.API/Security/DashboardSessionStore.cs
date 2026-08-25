using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Options;
using Vortex.Observability.Configuration;
using Vortex.Observability.Diagnostics;
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
internal sealed class DashboardSessionStore : IAccountSessionRevoker, IDisposable
{
    public string SessionKind => "dashboard";

    private readonly AccountSessionStore<DashboardSessionState> _sessions;
    private readonly Meter? _meter;
    private readonly Counter<long>? _revoked;

    public DashboardSessionStore(IOptions<ObservabilityConfig> options, IMeterFactory meterFactory)
    {
        int minutes = Math.Max(5, options.Value.DashboardSessionLifetimeMinutes);
        _sessions = new AccountSessionStore<DashboardSessionState>(TimeSpan.FromMinutes(minutes));

        if (!options.Value.MetricsEnabled)
        {
            return;
        }

        // Both instruments live here rather than on IVortexMetrics: the live count is only
        // answerable by a callback into this store, and splitting the pair would put two metric
        // dependencies in a class this small. ConnectionMetrics publishes the gateway's counts the
        // same way.
        _meter = meterFactory.Create(VortexTelemetry.Name, VortexTelemetry.Version);

        _meter.CreateObservableGauge(
            "Vortex.dashboard.session.active",
            () => _sessions.Count,
            unit: "{session}",
            description: "Dashboard sessions currently held, authenticated operators only."
        );

        _revoked = _meter.CreateCounter<long>(
            "Vortex.dashboard.session.revoked",
            unit: "{session}",
            description: "Dashboard sessions ended before their expiry, by reason."
        );
    }

    public int LifetimeSeconds => _sessions.LifetimeSeconds;

    public int Count => _sessions.Count;

    public string Create(int accountId, string email) =>
        _sessions.Create(accountId, new DashboardSessionState(email, null));

    public (int AccountId, string Email)? Resolve(string? sessionId)
    {
        (int AccountId, DashboardSessionState State)? found = _sessions.Resolve(sessionId);

        return found is null ? null : (found.Value.AccountId, found.Value.State.Email);
    }

    /// <summary>
    ///     When this session last proved a second factor, or null if it never has. Read by the
    ///     step-up filter; the freshness window itself is the filter's business, not the store's.
    /// </summary>
    public DateTime? SteppedUpAtUtc(string? sessionId) =>
        _sessions.Resolve(sessionId)?.State.SteppedUpAtUtc;

    /// <summary>
    ///     Stamps the session as having just proved a second factor. False when the token is unknown
    ///     or expired -- writing to a dead session must not revive it, which is
    ///     <see cref="AccountSessionStore{TState}.TryUpdate" />'s rule and the reason this goes
    ///     through it rather than replacing the entry.
    /// </summary>
    /// <param name="atUtc">
    ///     When the factor was proved. Defaults to now, and exists so a test can stamp a step-up that
    ///     has already aged out -- the alternative is a clock abstraction for one subtraction, and the
    ///     rule worth testing (a stale stamp does not pass) is otherwise unreachable in any test that
    ///     does not sleep.
    /// </param>
    public bool MarkSteppedUp(string? sessionId, DateTime? atUtc = null) =>
        _sessions.TryUpdate(
            sessionId,
            state => state with { SteppedUpAtUtc = atUtc ?? DateTime.UtcNow }
        );

    public void Remove(string? sessionId)
    {
        _sessions.Remove(sessionId);

        // Counted unconditionally: Remove is only ever reached from logout, and a call naming a
        // session that has already expired is the same operator ending the same session.
        Revoked("logout");
    }

    /// <summary>
    ///     Revokes every session of an account. Capability changes already take effect on the next
    ///     request, but a credential change does not: the cookie is what proves the operator, and it
    ///     keeps proving it until this is called.
    /// </summary>
    public int RemoveAllForAccount(int accountId)
    {
        int removed = _sessions.RemoveAllForAccount(accountId);

        for (int i = 0; i < removed; i++)
        {
            Revoked("credential-change");
        }

        return removed;
    }

    private void Revoked(string reason) =>
        _revoked?.Add(1, new KeyValuePair<string, object?>("reason", reason));

    public void Dispose() => _meter?.Dispose();
}

/// <summary>
///     What a dashboard session remembers besides its account: the operator's email for audit
///     attribution, and when this session last proved a second factor.
/// </summary>
/// <remarks>
///     The step-up stamp is per <em>session</em>, not per account. That is the whole point of it: a
///     second window opened with a stolen cookie has not stepped up merely because the real operator
///     did so in theirs, and one that has stepped up loses it when the session ends rather than
///     lingering on the account.
/// </remarks>
internal readonly record struct DashboardSessionState(string Email, DateTime? SteppedUpAtUtc);
