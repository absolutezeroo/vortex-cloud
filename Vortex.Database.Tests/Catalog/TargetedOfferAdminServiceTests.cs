using System;
using System.Collections.Immutable;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Orleans;
using Vortex.Catalog;
using Vortex.Database.Context;
using Vortex.Database.Entities.Catalog;
using Vortex.Primitives.Catalog.Admin;
using Vortex.Primitives.Catalog.Grains;
using Vortex.Primitives.Catalog.Snapshots;
using Xunit;

namespace Vortex.Database.Tests.Catalog;

/// <summary>
/// Covers the invariants <c>TargetedOfferAdminService</c> is responsible for: identifier is required,
/// unknown ids are rejected, an offer that has been purchased cannot be hard-deleted (the FK to
/// player state is Restrict — deactivate instead), and every successful write reloads the live offer
/// cache -- the "DB write not reflected in live state" bug class called out in AGENTS.md.
/// </summary>
public sealed class TargetedOfferAdminServiceTests
{
    private static DbContextOptions<VortexDbContext> NewOptions() =>
        new DbContextOptionsBuilder<VortexDbContext>()
            .UseInMemoryDatabase($"targeted-offer-admin-{Guid.NewGuid():N}")
            .Options;

    private static TargetedOfferAdminService NewService(
        DbContextOptions<VortexDbContext> options,
        out FakeTargetedOfferManagerGrain manager
    )
    {
        manager = new FakeTargetedOfferManagerGrain();
        IGrainFactory grainFactory = DispatchProxy.Create<IGrainFactory, GrainFactoryProxy>();
        ((GrainFactoryProxy)(object)grainFactory).Manager = manager;

        return new TargetedOfferAdminService(
            new TestDbContextFactory(options),
            grainFactory,
            NullLogger<TargetedOfferAdminService>.Instance
        );
    }

    private static TargetedOfferCreateSpec SampleOffer(string identifier = "starter_bundle") =>
        new(
            identifier,
            0,
            "Starter Bundle",
            "A great deal",
            "image",
            "icon",
            "starter",
            25,
            0,
            0,
            1,
            null,
            true,
            0
        );

    [Fact]
    public async Task CreateOffer_Succeeds_AndReloadsCache()
    {
        DbContextOptions<VortexDbContext> options = NewOptions();
        TargetedOfferAdminService service = NewService(
            options,
            out FakeTargetedOfferManagerGrain manager
        );

        CatalogAdminResult result = await service
            .CreateOfferAsync(SampleOffer(), CancellationToken.None)
            .ConfigureAwait(true);

        result.Success.Should().BeTrue();
        result.Id.Should().NotBeNull();

        await using VortexDbContext verify = new(options);
        (await verify.TargetedOffers.FindAsync(result.Id!.Value).ConfigureAwait(true))
            .Should()
            .NotBeNull();

        manager.ReloadCount.Should().Be(1);
    }

    [Fact]
    public async Task CreateOffer_BlankIdentifier_IsRejected_AndDoesNotReload()
    {
        DbContextOptions<VortexDbContext> options = NewOptions();
        TargetedOfferAdminService service = NewService(
            options,
            out FakeTargetedOfferManagerGrain manager
        );

        CatalogAdminResult result = await service
            .CreateOfferAsync(SampleOffer("   "), CancellationToken.None)
            .ConfigureAwait(true);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("identifier_required");
        manager.ReloadCount.Should().Be(0);
    }

    [Fact]
    public async Task UpdateOffer_UnknownId_ReturnsNotFound()
    {
        DbContextOptions<VortexDbContext> options = NewOptions();
        TargetedOfferAdminService service = NewService(options, out _);

        CatalogAdminResult result = await service
            .UpdateOfferAsync(
                999,
                new TargetedOfferUpdateSpec(
                    "whatever",
                    0,
                    "t",
                    "d",
                    "i",
                    "ic",
                    "pc",
                    10,
                    0,
                    0,
                    1,
                    null,
                    true,
                    0
                ),
                CancellationToken.None
            )
            .ConfigureAwait(true);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("offer_not_found");
    }

    [Fact]
    public async Task DeleteOffer_WithPurchases_IsBlocked()
    {
        DbContextOptions<VortexDbContext> options = NewOptions();
        TargetedOfferAdminService service = NewService(
            options,
            out FakeTargetedOfferManagerGrain manager
        );

        CatalogAdminResult offer = await service
            .CreateOfferAsync(SampleOffer(), CancellationToken.None)
            .ConfigureAwait(true);

        await using (VortexDbContext seed = new(options))
        {
            seed.PlayerTargetedOffers.Add(
                new PlayerTargetedOfferEntity
                {
                    PlayerEntityId = 1,
                    TargetedOfferEntityId = offer.Id!.Value,
                    PurchaseCount = 1,
                }
            );
            await seed.SaveChangesAsync().ConfigureAwait(true);
        }

        int reloadsBeforeDelete = manager.ReloadCount;

        CatalogAdminResult deleteResult = await service
            .DeleteOfferAsync(offer.Id!.Value, CancellationToken.None)
            .ConfigureAwait(true);

        deleteResult.Success.Should().BeFalse();
        deleteResult.ErrorCode.Should().Be("offer_has_purchases");
        // A blocked delete must not touch the live cache.
        manager.ReloadCount.Should().Be(reloadsBeforeDelete);
    }

    [Fact]
    public async Task DeleteOffer_WithoutPurchases_Succeeds_AndReloads()
    {
        DbContextOptions<VortexDbContext> options = NewOptions();
        TargetedOfferAdminService service = NewService(
            options,
            out FakeTargetedOfferManagerGrain manager
        );

        CatalogAdminResult offer = await service
            .CreateOfferAsync(SampleOffer(), CancellationToken.None)
            .ConfigureAwait(true);

        int reloadsBeforeDelete = manager.ReloadCount;

        CatalogAdminResult deleteResult = await service
            .DeleteOfferAsync(offer.Id!.Value, CancellationToken.None)
            .ConfigureAwait(true);

        deleteResult.Success.Should().BeTrue();
        manager.ReloadCount.Should().Be(reloadsBeforeDelete + 1);

        await using VortexDbContext verify = new(options);
        (await verify.TargetedOffers.FindAsync(offer.Id!.Value).ConfigureAwait(true))
            .Should()
            .BeNull();
    }

    [Fact]
    public async Task CreateProduct_UnknownFurnitureDefinition_IsRejected()
    {
        DbContextOptions<VortexDbContext> options = NewOptions();
        TargetedOfferAdminService service = NewService(options, out _);

        CatalogAdminResult offer = await service
            .CreateOfferAsync(SampleOffer(), CancellationToken.None)
            .ConfigureAwait(true);

        CatalogAdminResult result = await service
            .CreateProductAsync(
                new TargetedOfferProductCreateSpec(offer.Id!.Value, "chair", 424242, 1),
                CancellationToken.None
            )
            .ConfigureAwait(true);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("furniture_definition_not_found");
    }

    [Fact]
    public async Task CreateProduct_UnknownOffer_IsRejected()
    {
        DbContextOptions<VortexDbContext> options = NewOptions();
        TargetedOfferAdminService service = NewService(options, out _);

        CatalogAdminResult result = await service
            .CreateProductAsync(
                new TargetedOfferProductCreateSpec(999, "chair", null, 1),
                CancellationToken.None
            )
            .ConfigureAwait(true);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("offer_not_found");
    }

    private sealed class TestDbContextFactory(DbContextOptions<VortexDbContext> options)
        : IDbContextFactory<VortexDbContext>
    {
        public VortexDbContext CreateDbContext() => new(options);
    }

    private sealed class FakeTargetedOfferManagerGrain : ITargetedOfferManagerGrain
    {
        public int ReloadCount { get; private set; }

        public Task<ImmutableArray<TargetedOfferDefinitionSnapshot>> GetDefinitionsAsync(
            CancellationToken ct
        ) => Task.FromResult(ImmutableArray<TargetedOfferDefinitionSnapshot>.Empty);

        public Task ReloadAsync(CancellationToken ct)
        {
            ReloadCount++;
            return Task.CompletedTask;
        }
    }

    private class GrainFactoryProxy : DispatchProxy
    {
        public FakeTargetedOfferManagerGrain Manager { get; set; } = default!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (
                targetMethod is not null
                && targetMethod.Name == "GetGrain"
                && targetMethod.GetGenericArguments()[0] == typeof(ITargetedOfferManagerGrain)
            )
            {
                return Manager;
            }

            throw new NotSupportedException(targetMethod?.Name);
        }
    }
}
