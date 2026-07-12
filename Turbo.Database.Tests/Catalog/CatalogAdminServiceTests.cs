using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Turbo.Catalog;
using Turbo.Database.Context;
using Turbo.Primitives.Catalog;
using Turbo.Primitives.Catalog.Admin;
using Turbo.Primitives.Catalog.Enums;
using Turbo.Primitives.Catalog.Providers;
using Turbo.Primitives.Catalog.Snapshots;
using Turbo.Primitives.Catalog.Tags;
using Turbo.Primitives.Furniture.Enums;
using Xunit;

namespace Turbo.Database.Tests.Catalog;

/// <summary>
/// Covers the invariants <c>CatalogAdminService</c> is responsible for: the CatalogType boundary
/// between Normal/Builders Club pages (mirrors <see cref="CatalogTypeIsolationTests"/>'s read-side
/// coverage on the write side), the delete-blocked-with-children guards that prevent a silent
/// cascading mass-delete, and that every successful write reloads BOTH snapshot providers -- the
/// exact "DB write not reflected in live state" bug class called out in AGENTS.md.
/// </summary>
public sealed class CatalogAdminServiceTests
{
    private static TurboDbContext NewContext(DbContextOptions<TurboDbContext> options) =>
        new(options);

    private static DbContextOptions<TurboDbContext> NewOptions() =>
        new DbContextOptionsBuilder<TurboDbContext>()
            .UseInMemoryDatabase($"catalog-admin-{Guid.NewGuid():N}")
            .Options;

    private static CatalogAdminService NewService(
        DbContextOptions<TurboDbContext> options,
        out FakeSnapshotProvider<NormalCatalog> normalProvider,
        out FakeSnapshotProvider<BuildersClubCatalog> bcProvider
    )
    {
        normalProvider = new FakeSnapshotProvider<NormalCatalog>(CatalogType.Normal);
        bcProvider = new FakeSnapshotProvider<BuildersClubCatalog>(CatalogType.BuildersClub);

        return new CatalogAdminService(
            new TestDbContextFactory(options),
            normalProvider,
            bcProvider,
            NullLogger<CatalogAdminService>.Instance
        );
    }

    [Fact]
    public async Task CreatePage_Succeeds_AndReloadsBothSnapshotProviders()
    {
        DbContextOptions<TurboDbContext> options = NewOptions();
        CatalogAdminService service = NewService(
            options,
            out FakeSnapshotProvider<NormalCatalog> normalProvider,
            out FakeSnapshotProvider<BuildersClubCatalog> bcProvider
        );

        CatalogAdminResult result = await service
            .CreatePageAsync(
                new CatalogPageCreateSpec(
                    CatalogType.Normal,
                    null,
                    "shop_root",
                    "Shop",
                    0,
                    CatalogPageLayout.Default3x3,
                    null,
                    null,
                    0,
                    true
                ),
                CancellationToken.None
            )
            .ConfigureAwait(true);

        result.Success.Should().BeTrue();
        result.Id.Should().NotBeNull();

        await using TurboDbContext verify = NewContext(options);
        (await verify.CatalogPages.FindAsync(result.Id!.Value).ConfigureAwait(true))
            .Should()
            .NotBeNull();

        // Both providers reload unconditionally -- the page's own CatalogType decides which tree it
        // is visible in, but working that out per write path is exactly the class of bug this
        // simplification avoids (see the class doc on CatalogAdminService).
        normalProvider.ReloadCount.Should().Be(1);
        bcProvider.ReloadCount.Should().Be(1);
    }

    [Fact]
    public async Task CreatePage_UnderParentOfDifferentCatalogType_IsRejected()
    {
        DbContextOptions<TurboDbContext> options = NewOptions();
        CatalogAdminService service = NewService(options, out _, out _);

        CatalogAdminResult bcRoot = await service
            .CreatePageAsync(
                new CatalogPageCreateSpec(
                    CatalogType.BuildersClub,
                    null,
                    "bc_root",
                    null,
                    0,
                    CatalogPageLayout.Default3x3,
                    null,
                    null,
                    0,
                    true
                ),
                CancellationToken.None
            )
            .ConfigureAwait(true);

        CatalogAdminResult child = await service
            .CreatePageAsync(
                new CatalogPageCreateSpec(
                    CatalogType.Normal,
                    bcRoot.Id,
                    "normal_child_under_bc_parent",
                    null,
                    0,
                    CatalogPageLayout.Default3x3,
                    null,
                    null,
                    0,
                    true
                ),
                CancellationToken.None
            )
            .ConfigureAwait(true);

        child.Success.Should().BeFalse();
        child.ErrorCode.Should().Be("parent_catalog_type_mismatch");
    }

    [Fact]
    public async Task DeletePage_WithChildren_IsBlocked()
    {
        DbContextOptions<TurboDbContext> options = NewOptions();
        CatalogAdminService service = NewService(options, out _, out _);

        CatalogAdminResult parent = await service
            .CreatePageAsync(
                new CatalogPageCreateSpec(
                    CatalogType.Normal,
                    null,
                    "parent",
                    null,
                    0,
                    CatalogPageLayout.Default3x3,
                    null,
                    null,
                    0,
                    true
                ),
                CancellationToken.None
            )
            .ConfigureAwait(true);

        await service
            .CreatePageAsync(
                new CatalogPageCreateSpec(
                    CatalogType.Normal,
                    parent.Id,
                    "child",
                    null,
                    0,
                    CatalogPageLayout.Default3x3,
                    null,
                    null,
                    0,
                    true
                ),
                CancellationToken.None
            )
            .ConfigureAwait(true);

        CatalogAdminResult deleteResult = await service
            .DeletePageAsync(parent.Id!.Value, CancellationToken.None)
            .ConfigureAwait(true);

        deleteResult.Success.Should().BeFalse();
        deleteResult.ErrorCode.Should().Be("page_has_children");
    }

    [Fact]
    public async Task DeletePage_WithOffers_IsBlocked()
    {
        DbContextOptions<TurboDbContext> options = NewOptions();
        CatalogAdminService service = NewService(options, out _, out _);

        CatalogAdminResult page = await service
            .CreatePageAsync(
                new CatalogPageCreateSpec(
                    CatalogType.Normal,
                    null,
                    "page_with_offer",
                    null,
                    0,
                    CatalogPageLayout.Default3x3,
                    null,
                    null,
                    0,
                    true
                ),
                CancellationToken.None
            )
            .ConfigureAwait(true);

        await service
            .CreateOfferAsync(
                new CatalogOfferCreateSpec(
                    page.Id!.Value,
                    "offer_loc",
                    10,
                    0,
                    null,
                    true,
                    true,
                    0,
                    0,
                    true
                ),
                CancellationToken.None
            )
            .ConfigureAwait(true);

        CatalogAdminResult deleteResult = await service
            .DeletePageAsync(page.Id.Value, CancellationToken.None)
            .ConfigureAwait(true);

        deleteResult.Success.Should().BeFalse();
        deleteResult.ErrorCode.Should().Be("page_has_offers");
    }

    [Fact]
    public async Task DeleteOffer_WithProducts_IsBlocked_ThenSucceedsAfterProductRemoved()
    {
        DbContextOptions<TurboDbContext> options = NewOptions();
        CatalogAdminService service = NewService(options, out _, out _);

        CatalogAdminResult page = await service
            .CreatePageAsync(
                new CatalogPageCreateSpec(
                    CatalogType.Normal,
                    null,
                    "page",
                    null,
                    0,
                    CatalogPageLayout.Default3x3,
                    null,
                    null,
                    0,
                    true
                ),
                CancellationToken.None
            )
            .ConfigureAwait(true);

        CatalogAdminResult offer = await service
            .CreateOfferAsync(
                new CatalogOfferCreateSpec(
                    page.Id!.Value,
                    "offer_loc",
                    10,
                    0,
                    null,
                    true,
                    true,
                    0,
                    0,
                    true
                ),
                CancellationToken.None
            )
            .ConfigureAwait(true);

        CatalogAdminResult product = await service
            .CreateProductAsync(
                new CatalogProductCreateSpec(
                    offer.Id!.Value,
                    ProductType.Floor,
                    null,
                    null,
                    1,
                    0,
                    0,
                    false
                ),
                CancellationToken.None
            )
            .ConfigureAwait(true);

        product.Success.Should().BeTrue();

        CatalogAdminResult blockedDelete = await service
            .DeleteOfferAsync(offer.Id.Value, CancellationToken.None)
            .ConfigureAwait(true);

        blockedDelete.Success.Should().BeFalse();
        blockedDelete.ErrorCode.Should().Be("offer_has_products");

        (
            await service
                .DeleteProductAsync(product.Id!.Value, CancellationToken.None)
                .ConfigureAwait(true)
        )
            .Success.Should()
            .BeTrue();

        (
            await service
                .DeleteOfferAsync(offer.Id.Value, CancellationToken.None)
                .ConfigureAwait(true)
        )
            .Success.Should()
            .BeTrue();
    }

    [Fact]
    public async Task UpdatePage_UnknownId_ReturnsNotFound()
    {
        DbContextOptions<TurboDbContext> options = NewOptions();
        CatalogAdminService service = NewService(options, out _, out _);

        CatalogAdminResult result = await service
            .UpdatePageAsync(
                999,
                new CatalogPageUpdateSpec(
                    null,
                    "whatever",
                    null,
                    0,
                    CatalogPageLayout.Default3x3,
                    null,
                    null,
                    0,
                    true
                ),
                CancellationToken.None
            )
            .ConfigureAwait(true);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("page_not_found");
    }

    private sealed class TestDbContextFactory(DbContextOptions<TurboDbContext> options)
        : IDbContextFactory<TurboDbContext>
    {
        public TurboDbContext CreateDbContext() => new(options);
    }

    private sealed class FakeSnapshotProvider<TTag>(CatalogType catalogType)
        : ICatalogSnapshotProvider<TTag>
        where TTag : ICatalogTag
    {
        public int ReloadCount { get; private set; }

        public CatalogType CatalogType => catalogType;

        public CatalogSnapshot Current => CatalogSnapshot.Empty;

        public Task<CatalogSnapshot> GetSnapshotAsync(CancellationToken ct) =>
            Task.FromResult(CatalogSnapshot.Empty);

        public Task ReloadAsync(CancellationToken ct)
        {
            ReloadCount++;
            return Task.CompletedTask;
        }
    }
}
