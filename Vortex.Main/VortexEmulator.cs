using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Vortex.Primitives.Hosting;
using Vortex.Primitives.Networking;

namespace Vortex.Main;

public class VortexEmulator(
    ILogger<VortexEmulator> logger,
    IEnumerable<IReferenceDataProvider> referenceDataProviders,
    INetworkManager networkManager
) : IHostedService
{
    private readonly ILogger<VortexEmulator> _logger = logger;
    private readonly IEnumerable<IReferenceDataProvider> _referenceDataProviders =
        referenceDataProviders;
    private readonly INetworkManager _networkManager = networkManager;

    public async Task StartAsync(CancellationToken ct)
    {
        try
        {
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
