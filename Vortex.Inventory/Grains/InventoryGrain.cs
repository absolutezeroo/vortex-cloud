using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Vortex.Database.Context;
using Vortex.Inventory.Configuration;
using Vortex.Inventory.Grains.Modules;
using Vortex.Primitives.Events;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Inventory.Factories;
using Vortex.Primitives.Inventory.Grains;

namespace Vortex.Inventory.Grains;

public sealed partial class InventoryGrain : Grain, IInventoryGrain
{
    private readonly IDbContextFactory<VortexDbContext> _dbCtxFactory;
    private readonly InventoryConfig _inventoryConfig;
    private readonly IGrainFactory _grainFactory;
    private readonly IFurnitureDefinitionProvider _furnitureDefinitionProvider;
    private readonly IInventoryFurnitureLoader _furnitureItemsLoader;
    private readonly IStuffDataFactory _stuffDataFactory;
    private readonly IEventPublisher _events;
    private readonly ILogger<InventoryGrain> _logger;

    private readonly InventoryLiveState _state;
    private readonly InventoryFurniModule _furniModule;

    public InventoryGrain(
        IDbContextFactory<VortexDbContext> dbContextFactory,
        IOptions<InventoryConfig> inventoryConfig,
        IGrainFactory grainFactory,
        IFurnitureDefinitionProvider furnitureDefinitionProvider,
        IInventoryFurnitureLoader furnitureItemsLoader,
        IStuffDataFactory stuffDataFactory,
        IEventPublisher events,
        ILogger<InventoryGrain> logger
    )
    {
        _dbCtxFactory = dbContextFactory;
        _inventoryConfig = inventoryConfig.Value;
        _grainFactory = grainFactory;
        _furnitureDefinitionProvider = furnitureDefinitionProvider;
        _furnitureItemsLoader = furnitureItemsLoader;
        _stuffDataFactory = stuffDataFactory;
        _events = events;
        _logger = logger;

        _state = new();
        _furniModule = new InventoryFurniModule(this, _state, _furnitureItemsLoader);
    }

    public override Task OnActivateAsync(CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken ct)
    {
        return Task.CompletedTask;
    }
}
