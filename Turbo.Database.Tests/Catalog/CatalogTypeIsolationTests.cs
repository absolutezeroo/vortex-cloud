using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Turbo.Database.Context;
using Turbo.Database.Entities.Catalog;
using Turbo.Primitives.Catalog;
using Turbo.Primitives.Catalog.Enums;
using Turbo.Primitives.Furniture.Enums;
using Xunit;

namespace Turbo.Database.Tests.Catalog;

/// <summary>
/// Builders Club and Normal share the same catalog_pages/catalog_offers/catalog_products tables,
/// discriminated by CatalogPageEntity.CatalogType (not two separate tables). This is the invariant
/// that keeps that safe: filtering pages by CatalogType, then cascading to only the offers/products
/// under those pages, must never let one type's rows leak into the other's query result --
/// CatalogSnapshotProvider.ReloadAsync relies on exactly this filter shape.
/// </summary>
public sealed class CatalogTypeIsolationTests
{
    private static TurboDbContext NewContext() =>
        new(
            new DbContextOptionsBuilder<TurboDbContext>()
                .UseInMemoryDatabase($"catalog-isolation-{Guid.NewGuid():N}")
                .Options
        );

    [Fact]
    public void FilteringPagesByCatalogType_NeverLeaksTheOtherTypesPage()
    {
        using TurboDbContext context = NewContext();

        CatalogPageEntity normalPage = new()
        {
            CatalogType = CatalogType.Normal,
            Localization = "normal_page",
            Icon = 0,
            Layout = CatalogPageLayout.Default3x3,
            SortOrder = 0,
            Visible = true,
        };
        CatalogPageEntity bcPage = new()
        {
            CatalogType = CatalogType.BuildersClub,
            Localization = "bc_page",
            Icon = 0,
            Layout = CatalogPageLayout.Default3x3,
            SortOrder = 0,
            Visible = true,
        };

        context.CatalogPages.AddRange(normalPage, bcPage);
        context.SaveChanges();

        List<CatalogPageEntity> normalPages = context
            .CatalogPages.Where(p => p.CatalogType == CatalogType.Normal)
            .ToList();
        List<CatalogPageEntity> bcPages = context
            .CatalogPages.Where(p => p.CatalogType == CatalogType.BuildersClub)
            .ToList();

        normalPages.Should().ContainSingle(p => p.Id == normalPage.Id);
        normalPages.Should().NotContain(p => p.Id == bcPage.Id);
        bcPages.Should().ContainSingle(p => p.Id == bcPage.Id);
        bcPages.Should().NotContain(p => p.Id == normalPage.Id);
    }

    [Fact]
    public void FilteringCascadesToOffersAndProducts_NeverLeaksAcrossCatalogTypes()
    {
        using TurboDbContext context = NewContext();

        CatalogPageEntity normalPage = new()
        {
            CatalogType = CatalogType.Normal,
            Localization = "normal_page",
            Icon = 0,
            Layout = CatalogPageLayout.Default3x3,
            SortOrder = 0,
            Visible = true,
        };
        CatalogPageEntity bcPage = new()
        {
            CatalogType = CatalogType.BuildersClub,
            Localization = "bc_page",
            Icon = 0,
            Layout = CatalogPageLayout.Default3x3,
            SortOrder = 0,
            Visible = true,
        };
        context.CatalogPages.AddRange(normalPage, bcPage);
        context.SaveChanges();

        CatalogOfferEntity normalOffer = new()
        {
            CatalogPageEntityId = normalPage.Id,
            Page = normalPage,
            LocalizationId = "normal_offer",
            CostCredits = 10,
            CostCurrency = 0,
            CanGift = true,
            CanBundle = true,
            ClubLevel = 0,
            Visible = true,
        };
        CatalogOfferEntity bcOffer = new()
        {
            CatalogPageEntityId = bcPage.Id,
            Page = bcPage,
            LocalizationId = "bc_offer",
            CostCredits = 0,
            CostCurrency = 0,
            CanGift = false,
            CanBundle = false,
            ClubLevel = 0,
            Visible = true,
        };
        context.CatalogOffers.AddRange(normalOffer, bcOffer);
        context.SaveChanges();

        CatalogProductEntity normalProduct = new()
        {
            CatalogOfferEntityId = normalOffer.Id,
            Offer = normalOffer,
            ProductType = ProductType.Floor,
            Quantity = 1,
            UniqueSize = 0,
            UniqueRemaining = 0,
            BuildersClubEligible = false,
        };
        CatalogProductEntity bcProduct = new()
        {
            CatalogOfferEntityId = bcOffer.Id,
            Offer = bcOffer,
            ProductType = ProductType.Floor,
            Quantity = 1,
            UniqueSize = 0,
            UniqueRemaining = 0,
            BuildersClubEligible = true,
        };
        context.CatalogProducts.AddRange(normalProduct, bcProduct);
        context.SaveChanges();

        // Reproduces CatalogSnapshotProvider.ReloadAsync's exact cascade: pages -> offer ids -> product ids.
        HashSet<int> bcPageIds = context
            .CatalogPages.Where(p => p.CatalogType == CatalogType.BuildersClub)
            .Select(p => p.Id)
            .ToHashSet();
        List<CatalogOfferEntity> bcOffers = context
            .CatalogOffers.Where(o => bcPageIds.Contains(o.CatalogPageEntityId))
            .ToList();
        HashSet<int> bcOfferIds = bcOffers.Select(o => o.Id).ToHashSet();
        List<CatalogProductEntity> bcProducts = context
            .CatalogProducts.Where(p => bcOfferIds.Contains(p.CatalogOfferEntityId))
            .ToList();

        bcOffers.Should().ContainSingle(o => o.Id == bcOffer.Id);
        bcOffers.Should().NotContain(o => o.Id == normalOffer.Id);
        bcProducts.Should().ContainSingle(p => p.Id == bcProduct.Id);
        bcProducts.Should().NotContain(p => p.Id == normalProduct.Id);
    }
}
