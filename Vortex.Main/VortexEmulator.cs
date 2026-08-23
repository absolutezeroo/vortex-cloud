using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Runtime;
using Vortex.Main.Configuration;
using Vortex.Primitives.Hosting;
using Vortex.Primitives.Networking;

namespace Vortex.Main;

public class VortexEmulator(
    ILogger<VortexEmulator> logger,
    IEnumerable<IReferenceDataProvider> referenceDataProviders,
    INetworkManager networkManager,
    IGrainFactory grainFactory,
    IOptions<OrleansHostConfig> orleansConfig
) : IHostedService
{
    private readonly ILogger<VortexEmulator> _logger = logger;
    private readonly IEnumerable<IReferenceDataProvider> _referenceDataProviders =
        referenceDataProviders;
    private readonly INetworkManager _networkManager = networkManager;
    private readonly IGrainFactory _grainFactory = grainFactory;
    private readonly OrleansHostConfig _orleansConfig = orleansConfig.Value;

    public async Task StartAsync(CancellationToken ct)
    {
        try
        {
            await RefuseAnUndeclaredSecondSiloAsync(ct).ConfigureAwait(false);

            IEnumerable<IGrouping<int, IReferenceDataProvider>> stages = _referenceDataProviders
                .GroupBy(p => p.LoadStage)
                .OrderBy(stage => stage.Key);

            foreach (IGrouping<int, IReferenceDataProvider> stage in stages)
            {
                await Task.WhenAll(stage.Select(p => p.ReloadAsync(ct))).ConfigureAwait(false);
            }

            await _networkManager.StartAsync(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Emulator startup was cancelled.");

            throw;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Emulator failed to start!");

            throw;
        }
    }

    /// <summary>
    ///     The clustering configuration can form a multi-silo cluster; the application semantics
    ///     cannot yet (see <see cref="OrleansHostConfig.MultiSiloReady"/> for what exactly goes
    ///     stale). Nothing about that failure mode is loud — the hotel keeps serving, with one node
    ///     handing out furniture definitions an admin replaced hours ago — so the second silo is
    ///     stopped here instead, where it is a startup error somebody reads.
    /// </summary>
    private async Task RefuseAnUndeclaredSecondSiloAsync(CancellationToken ct)
    {
        if (_orleansConfig.MultiSiloReady)
        {
            return;
        }

        Dictionary<SiloAddress, SiloStatus> hosts = await _grainFactory
            .GetGrain<IManagementGrain>(0)
            .GetHosts(onlyActive: true)
            .WaitAsync(ct)
            .ConfigureAwait(false);

        if (hosts.Count <= 1)
        {
            return;
        }

        throw new InvalidOperationException(
            $"{hosts.Count} active silos are in this cluster, and this build's caches, metrics and "
                + "room streams are silo-local: an admin catalog or furniture edit would reload one "
                + "node and leave the others serving stale definitions indefinitely. Make those "
                + "components cluster-aware, then set "
                + $"'{OrleansHostConfig.SECTION_NAME}:{nameof(OrleansHostConfig.MultiSiloReady)}' "
                + $"to true. Active silos: {string.Join(", ", hosts.Keys)}."
        );
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Vortex StopAsync called.");

        try
        {
            await _networkManager.StopAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop the network manager during shutdown.");
        }
    }
}
