using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Orleans;
using Vortex.Database.Context;
using Vortex.Database.Entities.Catalog;
using Vortex.Primitives.Catalog.Grains;
using Vortex.Primitives.Catalog.Snapshots;

namespace Vortex.Catalog.Grains;

/// <summary>
/// Loads every active targeted-offer definition (and its bundle products) once and caches it for the
/// lifetime of the kept-alive singleton, ordered by sort order then id.
/// </summary>
[KeepAlive]
internal sealed class TargetedOfferManagerGrain(
    IDbContextFactory<VortexDbContext> dbCtxFactory,
    ILogger<TargetedOfferManagerGrain> logger
) : Grain, ITargetedOfferManagerGrain
{
    private readonly IDbContextFactory<VortexDbContext> _dbCtxFactory = dbCtxFactory;
    private readonly ILogger<TargetedOfferManagerGrain> _logger = logger;

    private ImmutableArray<TargetedOfferDefinitionSnapshot> _definitions =
        ImmutableArray<TargetedOfferDefinitionSnapshot>.Empty;
    private bool _loaded;

    public override async Task OnActivateAsync(CancellationToken ct)
    {
        await LoadAsync(ct).ConfigureAwait(true);
        await base.OnActivateAsync(ct).ConfigureAwait(true);
    }

    public async Task<ImmutableArray<TargetedOfferDefinitionSnapshot>> GetDefinitionsAsync(
        CancellationToken ct
    )
    {
        if (!_loaded)
        {
            await LoadAsync(ct).ConfigureAwait(true);
        }

        return _definitions;
    }

    public Task ReloadAsync(CancellationToken ct) => LoadAsync(ct);

    private async Task LoadAsync(CancellationToken ct)
    {
        try
        {
            await using VortexDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            List<TargetedOfferEntity> offers = await dbCtx
                .TargetedOffers.AsNoTracking()
                .Where(o => o.Active)
                .OrderBy(o => o.SortOrder)
                .ThenBy(o => o.Id)
                .ToListAsync(ct)
                .ConfigureAwait(true);

            List<TargetedOfferProductEntity> products = await dbCtx
                .TargetedOfferProducts.AsNoTracking()
                .ToListAsync(ct)
                .ConfigureAwait(true);

            Dictionary<int, ImmutableArray<TargetedOfferProductSnapshot>> productsByOffer = products
                .GroupBy(p => p.TargetedOfferEntityId)
                .ToDictionary(
                    g => g.Key,
                    g =>
                        g.OrderBy(p => p.Id)
                            .Select(p => new TargetedOfferProductSnapshot
                            {
                                ProductCode = p.ProductCode,
                                FurnitureDefinitionId = p.FurnitureDefinitionEntityId,
                                Quantity = Math.Max(1, p.Quantity),
                            })
                            .ToImmutableArray()
                );

            _definitions =
            [
                .. offers.Select(o => new TargetedOfferDefinitionSnapshot
                {
                    Id = o.Id,
                    Identifier = o.Identifier,
                    OfferType = o.OfferType,
                    Title = o.Title,
                    Description = o.Description,
                    ImageUrl = o.ImageUrl,
                    IconImageUrl = o.IconImageUrl,
                    ProductCode = o.ProductCode,
                    PriceInCredits = o.PriceInCredits,
                    PriceInActivityPoints = o.PriceInActivityPoints,
                    ActivityPointType = o.ActivityPointType,
                    PurchaseLimit = o.PurchaseLimit,
                    ExpiresAt = o.ExpiresAt,
                    SortOrder = o.SortOrder,
                    Products = productsByOffer.GetValueOrDefault(
                        o.Id,
                        ImmutableArray<TargetedOfferProductSnapshot>.Empty
                    ),
                }),
            ];

            _loaded = true;
            _logger.LogInformation(
                "Loaded {Count} targeted offer definitions into cache.",
                _definitions.Length
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load targeted offer definitions.");
        }
    }
}
