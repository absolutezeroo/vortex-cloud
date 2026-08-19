using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Vortex.Supervisor.Configuration;

namespace Vortex.Supervisor.Health;

/// <summary>
///     Polls the emulator's <c>/health</c> endpoint so the panel can tell "the process is alive"
///     apart from "the hotel is serving". A process that is up but cannot reach the database is not
///     a running hotel, and the operator needs to see that difference during a restart.
/// </summary>
public sealed class EmulatorHealthProbe(
    IOptions<SupervisorConfig> config,
    IHttpClientFactory httpClientFactory
) : BackgroundService
{
    private readonly SupervisorConfig _config = config.Value;

    /// <summary>The last status word the emulator reported, or null when it did not answer.</summary>
    public string? LastStatus { get; private set; }

    public DateTimeOffset? LastCheckedUtc { get; private set; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(
            TimeSpan.FromSeconds(Math.Max(1, _config.HealthPollSeconds))
        );

        while (!stoppingToken.IsCancellationRequested)
        {
            await ProbeAsync(stoppingToken).ConfigureAwait(false);

            try
            {
                await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private async Task ProbeAsync(CancellationToken ct)
    {
        try
        {
            using HttpClient client = httpClientFactory.CreateClient("health");
            client.Timeout = TimeSpan.FromSeconds(3);

            using HttpResponseMessage response = await client
                .GetAsync(new Uri(_config.HealthUrl), ct)
                .ConfigureAwait(false);

            string body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

            LastStatus = response.IsSuccessStatusCode
                ? Summarise(body)
                : $"HTTP {(int)response.StatusCode}";
        }
        catch (Exception)
        {
            // Not reachable is the normal state while the emulator is down — a status, not an error.
            LastStatus = null;
        }

        LastCheckedUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    ///     The payload is a small JSON object; the panel only shows the status word, so pull it out
    ///     without taking a dependency on the emulator's response shape.
    /// </summary>
    private static string Summarise(string body)
    {
        foreach (string candidate in (string[])["Healthy", "Degraded", "Unhealthy"])
        {
            if (body.Contains(candidate, StringComparison.OrdinalIgnoreCase))
            {
                return candidate;
            }
        }

        return "Unknown";
    }
}
