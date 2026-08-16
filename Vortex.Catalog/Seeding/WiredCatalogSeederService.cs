using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Vortex.Database.Context;
using Vortex.Database.Entities.Catalog;
using Vortex.Database.Entities.Furniture;
using Vortex.Primitives.Catalog;
using Vortex.Primitives.Furniture.Enums;

namespace Vortex.Catalog.Seeding;

/// <summary>
/// Makes sure every wired box a room can be built with is actually purchasable from the catalogue's
/// wired section.
/// </summary>
/// <remarks>
/// Unlike the other seeders this one is not gated on "the table is empty": an imported catalogue is
/// full of wired furni and still leaves whole families unreachable — a box sold from a page nobody
/// browses, or whose offer was left hidden, cannot be bought, and the server-side box may as well
/// not exist. So the gate is per box and per section: a wired furni with no visible offer anywhere
/// under the wired root gets one on its family's page.
/// <para>
/// It is strictly additive. It never edits, hides, moves or prices an offer an operator already
/// made — a duplicate listing elsewhere in the catalogue is theirs to keep, and a box they hid on
/// purpose stays hidden where they hid it while gaining a listing where players look for it.
/// </para>
/// </remarks>
internal sealed class WiredCatalogSeederService(
    IDbContextFactory<VortexDbContext> dbContextFactory,
    ILogger<WiredCatalogSeederService> logger
) : IHostedService
{
    /// <summary>What every wired box in an imported Habbo catalogue costs, by a wide margin: 265 of
    /// the 294 priced wired offers in the reference data.</summary>
    private const int WiredBoxCostCredits = 3;

    /// <summary>Anything else on these pages sits at the far end of the catalogue's ordering, so a
    /// created family page lands beside its siblings rather than in the middle of them.</summary>
    private const int CreatedPageSortOffset = 300;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await SeedAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // A catalogue that could not be topped up is worth a loud line, not a failed boot.
            logger.LogError(ex, "Failed to seed the wired catalogue section.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task SeedAsync(CancellationToken ct)
    {
        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        CatalogPageEntity? root = await db
            .CatalogPages.FirstOrDefaultAsync(
                page =>
                    page.Localization == WiredCatalogCategories.RootLocalization
                    && page.DeletedAt == null,
                ct
            )
            .ConfigureAwait(false);

        if (root is null)
        {
            logger.LogInformation(
                "No '{Localization}' catalogue page: the wired section is not seeded.",
                WiredCatalogCategories.RootLocalization
            );

            return;
        }

        HashSet<int> section = await LoadSectionPageIdsAsync(db, root.Id, ct).ConfigureAwait(false);

        HashSet<string> alreadySold = await LoadClassNamesOnSaleAsync(db, section, ct)
            .ConfigureAwait(false);

        List<FurnitureDefinitionEntity> missing = await LoadUnsoldWiredDefinitionsAsync(
                db,
                alreadySold,
                ct
            )
            .ConfigureAwait(false);

        if (missing.Count == 0)
        {
            return;
        }

        Dictionary<string, CatalogPageEntity> pages = await EnsureFamilyPagesAsync(db, root, ct)
            .ConfigureAwait(false);

        int added = 0;

        foreach (FurnitureDefinitionEntity definition in missing)
        {
            WiredCatalogCategory? category = WiredCatalogCategories.ForClassName(definition.Name);

            if (
                category is null
                || !pages.TryGetValue(category.Localization, out CatalogPageEntity? page)
            )
            {
                continue;
            }

            CatalogOfferEntity offer = new()
            {
                CatalogPageEntityId = page.Id,
                Page = page,
                // The classname is the offer key an imported catalogue uses, and the client reads
                // its label from the same localization file the furni's name comes from.
                LocalizationId = definition.Name,
                CostCredits = WiredBoxCostCredits,
                CostCurrency = 0,
                CanGift = true,
                CanBundle = true,
                ClubLevel = 0,
                Visible = true,
            };

            db.CatalogOffers.Add(offer);

            db.CatalogProducts.Add(
                new CatalogProductEntity
                {
                    CatalogOfferEntityId = offer.Id,
                    Offer = offer,
                    // A wired box is whatever its own definition says it is; a couple of them
                    // (the doors) are wall items.
                    ProductType = definition.ProductType,
                    FurnitureDefinitionEntityId = definition.Id,
                    Quantity = 1,
                    UniqueSize = 0,
                    UniqueRemaining = 0,
                    BuildersClubEligible = false,
                }
            );

            added++;
        }

        if (added == 0)
        {
            return;
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "Wired catalogue: added {Added} box(es) that no page under '{Root}' was selling.",
            added,
            WiredCatalogCategories.RootLocalization
        );
    }

    /// <summary>Every page under the wired root, however deep — an imported catalogue nests the
    /// families under Habbo/Custom subpages, and a box sold there is already reachable.</summary>
    private static async Task<HashSet<int>> LoadSectionPageIdsAsync(
        VortexDbContext db,
        int rootId,
        CancellationToken ct
    )
    {
        var pages = await db
            .CatalogPages.AsNoTracking()
            .Where(page => page.DeletedAt == null && page.ParentEntityId != null)
            .Select(page => new { page.Id, ParentId = page.ParentEntityId!.Value })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        Dictionary<int, List<int>> childrenByParent = pages
            .GroupBy(page => page.ParentId)
            .ToDictionary(group => group.Key, group => group.Select(page => page.Id).ToList());

        HashSet<int> section = [rootId];
        Queue<int> pending = new([rootId]);

        while (pending.Count > 0)
        {
            if (!childrenByParent.TryGetValue(pending.Dequeue(), out List<int>? children))
            {
                continue;
            }

            foreach (int child in children.Where(section.Add))
            {
                pending.Enqueue(child);
            }
        }

        return section;
    }

    /// <summary>The classnames a player can already buy from somewhere in the wired section.</summary>
    private static async Task<HashSet<string>> LoadClassNamesOnSaleAsync(
        VortexDbContext db,
        HashSet<int> section,
        CancellationToken ct
    ) =>
        [
            .. await db
                .CatalogProducts.AsNoTracking()
                .Where(product =>
                    product.DeletedAt == null
                    && product.FurnitureDefinition != null
                    && product.Offer.DeletedAt == null
                    && product.Offer.Visible
                    && section.Contains(product.Offer.CatalogPageEntityId)
                )
                .Select(product => product.FurnitureDefinition!.Name)
                .Distinct()
                .ToListAsync(ct)
                .ConfigureAwait(false),
        ];

    /// <summary>
    /// One definition per wired classname that nothing in the section sells. Duplicate rows for the
    /// same classname are catalogue noise, and listing a furni twice on its own page would be too:
    /// the lowest id wins.
    /// </summary>
    private static async Task<List<FurnitureDefinitionEntity>> LoadUnsoldWiredDefinitionsAsync(
        VortexDbContext db,
        HashSet<string> alreadySold,
        CancellationToken ct
    )
    {
        List<FurnitureDefinitionEntity> wired = await db
            .FurnitureDefinitions.Where(definition =>
                definition.DeletedAt == null && definition.Name.StartsWith("wf_")
            )
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return
        [
            .. wired
                .Where(definition =>
                    WiredCatalogCategories.ForClassName(definition.Name) is not null
                    && !alreadySold.Contains(definition.Name)
                )
                .GroupBy(definition => definition.Name, StringComparer.Ordinal)
                .Select(group => group.OrderBy(definition => definition.Id).First())
                .OrderBy(definition => definition.Name, StringComparer.Ordinal),
        ];
    }

    /// <summary>The six family pages, creating only the ones the hotel does not already have as a
    /// direct child of the wired root.</summary>
    private static async Task<Dictionary<string, CatalogPageEntity>> EnsureFamilyPagesAsync(
        VortexDbContext db,
        CatalogPageEntity root,
        CancellationToken ct
    )
    {
        List<CatalogPageEntity> children = await db
            .CatalogPages.Where(page => page.ParentEntityId == root.Id && page.DeletedAt == null)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        Dictionary<string, CatalogPageEntity> pages = [];
        List<CatalogPageEntity> created = [];

        foreach (WiredCatalogCategory category in WiredCatalogCategories.All)
        {
            CatalogPageEntity? page = children.FirstOrDefault(child =>
                string.Equals(child.Localization, category.Localization, StringComparison.Ordinal)
            );

            if (page is null)
            {
                page = new CatalogPageEntity
                {
                    CatalogType = root.CatalogType,
                    ParentEntityId = root.Id,
                    Localization = category.Localization,
                    Name = category.Name,
                    Icon = root.Icon,
                    Layout = CatalogPageLayout.Default3x3,
                    ImageData = root.ImageData,
                    SortOrder = CreatedPageSortOffset + category.SortOrder,
                    Visible = true,
                };

                db.CatalogPages.Add(page);
                created.Add(page);
            }

            pages[category.Localization] = page;
        }

        if (created.Count > 0)
        {
            // The offers need real page ids, so the pages are written first.
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        return pages;
    }
}
