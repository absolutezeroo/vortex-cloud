using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Vortex.Primitives.Catalog.Providers;
using Vortex.Primitives.Catalog.Tags;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Groups.Providers;
using Vortex.Primitives.Marketplace.Providers;
using Vortex.Primitives.Navigator;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Networking.Revisions;
using Vortex.Primitives.Pets.Providers;
using Vortex.Primitives.Players.Providers;
using Vortex.Primitives.Rooms.Providers;
using Vortex.Revisions.Revision20260701;

namespace Vortex.Main;

public class VortexEmulator(
    ILogger<VortexEmulator> logger,
    IFurnitureDefinitionProvider furnitureProvider,
    ICatalogSnapshotProvider<NormalCatalog> catalogProvider,
    ICatalogSnapshotProvider<BuildersClubCatalog> buildersClubCatalogProvider,
    ICatalogClubOfferProvider clubOfferProvider,
    ICatalogClubGiftProvider clubGiftProvider,
    ICurrencyTypeProvider currencyTypeProvider,
    IGroupBadgePartProvider guildBadgePartProvider,
    IMarketplaceSettingsProvider marketplaceSettingsProvider,
    IPetPaletteProvider petPaletteProvider,
    IPetCommandProvider petCommandProvider,
    IPetLevelProvider petLevelProvider,
    INavigatorProvider topLevelContextProvider,
    IRoomModelProvider roomModelProvider,
    INetworkManager networkManager,
    IRevisionManager revisionManager,
    Revision20260701 defaultRevision
) : IHostedService
{
    private readonly ICatalogSnapshotProvider<BuildersClubCatalog> _buildersClubCatalogProvider =
        buildersClubCatalogProvider;

    private readonly ICatalogSnapshotProvider<NormalCatalog> _catalogProvider = catalogProvider;
    private readonly ICatalogClubGiftProvider _clubGiftProvider = clubGiftProvider;
    private readonly ICatalogClubOfferProvider _clubOfferProvider = clubOfferProvider;
    private readonly ICurrencyTypeProvider _currencyTypeProvider = currencyTypeProvider;
    private readonly Revision20260701 _defaultRevision = defaultRevision;
    private readonly IFurnitureDefinitionProvider _furnitureProvider = furnitureProvider;
    private readonly IGroupBadgePartProvider _guildBadgePartProvider = guildBadgePartProvider;
    private readonly ILogger<VortexEmulator> _logger = logger;
    private readonly IMarketplaceSettingsProvider _marketplaceSettingsProvider =
        marketplaceSettingsProvider;
    private readonly INetworkManager _networkManager = networkManager;
    private readonly IPetCommandProvider _petCommandProvider = petCommandProvider;
    private readonly IPetLevelProvider _petLevelProvider = petLevelProvider;
    private readonly IPetPaletteProvider _petPaletteProvider = petPaletteProvider;
    private readonly IRevisionManager _revisionManager = revisionManager;
    private readonly IRoomModelProvider _roomModelProvider = roomModelProvider;
    private readonly INavigatorProvider _topLevelContextProvider = topLevelContextProvider;

    public async Task StartAsync(CancellationToken ct)
    {
        try
        {
            _revisionManager.RegisterRevision(_defaultRevision);
            await _furnitureProvider.ReloadAsync(ct).ConfigureAwait(false);
            await _catalogProvider.ReloadAsync(ct).ConfigureAwait(false);
            await _buildersClubCatalogProvider.ReloadAsync(ct).ConfigureAwait(false);
            await _clubOfferProvider.ReloadAsync(ct).ConfigureAwait(false);
            await _clubGiftProvider.ReloadAsync(ct).ConfigureAwait(false);
            await _currencyTypeProvider.ReloadAsync(ct).ConfigureAwait(false);
            await _marketplaceSettingsProvider.ReloadAsync(ct).ConfigureAwait(false);
            await _guildBadgePartProvider.ReloadAsync(ct).ConfigureAwait(false);
            await _petPaletteProvider.ReloadAsync(ct).ConfigureAwait(false);
            await _petCommandProvider.ReloadAsync(ct).ConfigureAwait(false);
            await _petLevelProvider.ReloadAsync(ct).ConfigureAwait(false);
            await _topLevelContextProvider.ReloadAsync(ct).ConfigureAwait(false);
            await _roomModelProvider.ReloadAsync(ct).ConfigureAwait(false);
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
