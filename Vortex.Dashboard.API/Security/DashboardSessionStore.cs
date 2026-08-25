using System;
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

    private readonly AccountSessionStore<string> _sessions;
    private readonly Meter? _meter;
    private readonly Counter<long>? _revoked;

    public DashboardSessionStore(IOptions<ObservabilityConfig> options, IMeterFactory meterFactory)
    {
        int minutes = Math.Max(5, options.Value.DashboardSessionLifetimeMinutes);
        _sessions = new AccountSessionStore<string>(TimeSpan.FromMinutes(minutes));

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

    public string Create(int accountId, string email) => _sessions.Create(accountId, email);

    public (int AccountId, string Email)? Resolve(string? sessionId) =>
        _sessions.Resolve(sessionId);

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
        _revoked?.Add(
            1,
            new System.Collections.Generic.KeyValuePair<string, object?>("reason", reason)
        );

    public void Dispose() => _meter?.Dispose();
}
