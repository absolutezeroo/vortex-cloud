using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vortex.Database.Context;
using Vortex.Database.Entities.Audit;
using Vortex.Database.Entities.Errors;
using Vortex.Observability.Configuration;
using Vortex.Primitives.Observability;

namespace Vortex.Observability.Runtime;

public interface IIncidentDetectionService
{
    Task<IncidentDetectionSnapshot> DetectAsync(CancellationToken ct);
}

public sealed class IncidentDetectionService(
    IInfrastructureHealthService healthService,
    IDbContextFactory<TurboDbContext> dbContextFactory,
    IOptions<ObservabilityConfig> options,
    ILogger<IncidentDetectionService> logger
) : IIncidentDetectionService
{
    private readonly IInfrastructureHealthService _healthService = healthService;
    private readonly IDbContextFactory<TurboDbContext> _dbContextFactory = dbContextFactory;
    private readonly ObservabilityConfig _config = options.Value;
    private readonly ILogger<IncidentDetectionService> _logger = logger;
    private readonly int _lookbackMinutes = Math.Max(1, options.Value.IncidentLookbackMinutes);
    private readonly int _topErrorGroups = Math.Max(1, options.Value.IncidentTopErrorGroups);
    private readonly int _errorGroupWindowDays = Math.Max(
        1,
        options.Value.IncidentErrorGroupWindowDays
    );
    private readonly int _errorSpikesCritical = Math.Max(
        0,
        options.Value.ErrorSpikesCriticalPerMinute
    );
    private readonly int _errorSpikesDegraded = Math.Max(
        0,
        options.Value.ErrorSpikesDegradedPerMinute
    );
    private readonly int _loginFailedSpikesCritical = Math.Max(
        0,
        options.Value.LoginFailedSpikesCriticalPerMinute
    );
    private readonly int _loginFailedSpikesDegraded = Math.Max(
        0,
        options.Value.LoginFailedSpikesDegradedPerMinute
    );

    public async Task<IncidentDetectionSnapshot> DetectAsync(CancellationToken ct)
    {
        List<IncidentSignal> incidents = new List<IncidentSignal>();
        InfrastructureHealthSnapshot health = await _healthService
            .GetStatusAsync(ct)
            .ConfigureAwait(false);

        EvaluateHealthSignals(health, incidents);

        try
        {
            await using TurboDbContext db = await _dbContextFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(false);

            DateTime now = DateTime.UtcNow;
            DateTime since = now.AddMinutes(-_lookbackMinutes);
            double windowMinutes = Math.Max(1.0, (now - since).TotalMinutes);
            int errorCount = await db
                .ErrorOccurrences.AsNoTracking()
                .CountAsync(o => o.OccurredAt >= since, ct)
                .ConfigureAwait(false);

            int loginFailedCount = await db
                .AuditEvents.AsNoTracking()
                .CountAsync(
                    e =>
                        e.Category == AuditCategory.Auth
                        && e.Action == "auth.login.failed"
                        && e.OccurredAt >= since,
                    ct
                )
                .ConfigureAwait(false);

            DateTime groupSince = now.AddDays(-_errorGroupWindowDays);
            List<TopErrorGroupSnapshot> topGroups = await db
                .ErrorGroups.AsNoTracking()
                .Where(g => g.LastSeenAt >= groupSince)
                .OrderByDescending(g => g.TotalOccurrences)
                .Take(_topErrorGroups)
                .Select(g => new TopErrorGroupSnapshot(
                    g.Fingerprint,
                    g.Source,
                    g.Operation,
                    g.ExceptionType,
                    g.MessageSignature,
                    g.SampleMessage,
                    g.TotalOccurrences,
                    g.FirstSeenAt,
                    g.LastSeenAt,
                    g.LastActorPlayerId,
                    g.LastRoomId,
                    g.LastCorrelationId
                ))
                .ToListAsync(ct)
                .ConfigureAwait(false);

            double errorSpikes = errorCount / windowMinutes;
            double loginFailedSpikes = loginFailedCount / windowMinutes;

            EvaluateErrorSpikes(errorSpikes, incidents);
            EvaluateLoginFailedSpikes(loginFailedSpikes, incidents);

            return BuildSnapshot(incidents, topGroups, errorSpikes, loginFailedSpikes, now);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning(
                ex,
                "Incident detection failed; DB-backed analytics are unavailable for this snapshot."
            );
        }

        return BuildSnapshot(
            incidents,
            Array.Empty<TopErrorGroupSnapshot>(),
            0,
            0,
            DateTime.UtcNow
        );
    }

    private IncidentDetectionSnapshot BuildSnapshot(
        List<IncidentSignal> incidents,
        IReadOnlyList<TopErrorGroupSnapshot> topGroups,
        double errorSpikesPerMinute,
        double loginFailedSpikesPerMinute,
        DateTime generatedAt
    )
    {
        string overall = ComputeOverallSeverity(incidents);
        IncidentSignal[] ordered = incidents
            .OrderBy(i => GetSeverityRank(i.Severity))
            .ThenByDescending(i => i.DetectedAt)
            .ToArray();

        return new(
            overall,
            ordered,
            topGroups,
            errorSpikesPerMinute,
            loginFailedSpikesPerMinute,
            generatedAt
        );
    }

    private static string ComputeOverallSeverity(IReadOnlyCollection<IncidentSignal> incidents)
    {
        if (incidents.Any(i => i.Severity == "critical"))
        {
            return "Critical";
        }

        if (incidents.Any(i => i.Severity == "degraded"))
        {
            return "Degraded";
        }

        return "Healthy";
    }

    private static int GetSeverityRank(string severity) =>
        string.Equals(severity, "critical", StringComparison.OrdinalIgnoreCase) ? 0
        : string.Equals(severity, "degraded", StringComparison.OrdinalIgnoreCase) ? 1
        : 2;

    private void EvaluateHealthSignals(
        InfrastructureHealthSnapshot health,
        List<IncidentSignal> incidents
    )
    {
        if (IsCritical(health.Database.Status))
        {
            incidents.Add(
                new IncidentSignal(
                    "orleans.database.critical",
                    "critical",
                    "Database probe critical",
                    $"Database failed health probe with critical latency/error: {health.Database.Detail}",
                    health.Database.LatencyMs ?? 0,
                    _config.DatabaseProbeCriticalMs,
                    DateTime.UtcNow
                )
            );
        }
        else if (IsDegraded(health.Database.Status))
        {
            incidents.Add(
                new IncidentSignal(
                    "orleans.database.degraded",
                    "degraded",
                    "Database probe degraded",
                    $"Database is degraded: {health.Database.Detail}",
                    health.Database.LatencyMs ?? 0,
                    _config.DatabaseProbeDegradedMs,
                    DateTime.UtcNow
                )
            );
        }

        if (IsCritical(health.Orleans.Status))
        {
            incidents.Add(
                new IncidentSignal(
                    "orleans.cluster.critical",
                    "critical",
                    "Orleans probe critical",
                    $"Orleans probe is critical: {health.Orleans.Detail}",
                    health.Orleans.LatencyMs ?? 0,
                    _config.OrleansProbeCriticalMs,
                    DateTime.UtcNow
                )
            );
        }
        else if (IsDegraded(health.Orleans.Status))
        {
            incidents.Add(
                new IncidentSignal(
                    "orleans.cluster.degraded",
                    "degraded",
                    "Orleans probe degraded",
                    $"Orleans probe is degraded: {health.Orleans.Detail}",
                    health.Orleans.LatencyMs ?? 0,
                    _config.OrleansProbeDegradedMs,
                    DateTime.UtcNow
                )
            );
        }
    }

    private static bool IsCritical(string status) =>
        status.Equals("critical", StringComparison.OrdinalIgnoreCase);

    private static bool IsDegraded(string status) =>
        status.Equals("degraded", StringComparison.OrdinalIgnoreCase);

    private void EvaluateErrorSpikes(double errorSpikes, List<IncidentSignal> incidents)
    {
        if (errorSpikes >= _errorSpikesCritical)
        {
            incidents.Add(
                new IncidentSignal(
                    "runtime.error-spike.critical",
                    "critical",
                    "Runtime error spike",
                    $"Errors in last {_lookbackMinutes} min: {errorSpikes:0.0}/min.",
                    errorSpikes,
                    _errorSpikesCritical,
                    DateTime.UtcNow
                )
            );
            return;
        }

        if (errorSpikes >= _errorSpikesDegraded)
        {
            incidents.Add(
                new IncidentSignal(
                    "runtime.error-spike.degraded",
                    "degraded",
                    "Runtime error spike",
                    $"Errors in last {_config.IncidentLookbackMinutes} min: {errorSpikes:0.0}/min.",
                    errorSpikes,
                    _errorSpikesDegraded,
                    DateTime.UtcNow
                )
            );
        }
    }

    private void EvaluateLoginFailedSpikes(double loginFailedSpikes, List<IncidentSignal> incidents)
    {
        if (loginFailedSpikes >= _loginFailedSpikesCritical)
        {
            incidents.Add(
                new IncidentSignal(
                    "auth.login-failed.critical",
                    "critical",
                    "Authentication failures spike",
                    $"Failed logins in last {_lookbackMinutes} min: {loginFailedSpikes:0.0}/min.",
                    loginFailedSpikes,
                    _loginFailedSpikesCritical,
                    DateTime.UtcNow
                )
            );
            return;
        }

        if (loginFailedSpikes >= _loginFailedSpikesDegraded)
        {
            incidents.Add(
                new IncidentSignal(
                    "auth.login-failed.degraded",
                    "degraded",
                    "Authentication failures spike",
                    $"Failed logins in last {_config.IncidentLookbackMinutes} min: {loginFailedSpikes:0.0}/min.",
                    loginFailedSpikes,
                    _loginFailedSpikesDegraded,
                    DateTime.UtcNow
                )
            );
        }
    }
}

public sealed record IncidentDetectionSnapshot(
    string OverallSeverity,
    IReadOnlyList<IncidentSignal> Signals,
    IReadOnlyList<TopErrorGroupSnapshot> TopErrorGroups,
    double ErrorSpikesPerMinute,
    double LoginFailedSpikesPerMinute,
    DateTime GeneratedAt
);

public sealed record IncidentSignal(
    string Code,
    string Severity,
    string Title,
    string Summary,
    double Observed,
    double Threshold,
    DateTime DetectedAt
);

public sealed record TopErrorGroupSnapshot(
    string Fingerprint,
    string Source,
    string Operation,
    string ExceptionType,
    string MessageSignature,
    string SampleMessage,
    int TotalOccurrences,
    DateTime FirstSeenAt,
    DateTime LastSeenAt,
    long? LastActorPlayerId,
    int? LastRoomId,
    string? LastCorrelationId
);
