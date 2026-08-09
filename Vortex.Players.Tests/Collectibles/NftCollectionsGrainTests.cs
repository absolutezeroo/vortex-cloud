using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Vortex.Database.Context;
using Vortex.Database.Entities.Collectibles;
using Vortex.Database.Entities.Furniture;
using Vortex.Players.Grains;
using Vortex.Primitives.Collectibles;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Players;
using Vortex.Tests.Support;
using Xunit;

namespace Vortex.Players.Tests.Collectibles;

/// <summary>
/// Where a player stands in a collection is worked out from what they own, matched by classname.
/// The rules worth pinning down: owning six of one thing is not six times the collector, the boost
/// only lands on a finished set, and a best score does not fall when furniture is sold.
/// </summary>
public sealed class NftCollectionsGrainTests
{
    private static readonly PlayerId Collector = new(101);

    private const string Sofa = "club_sofa";
    private const string Throne = "throne";

    [Fact]
    public async Task ACollectionCountsOnlyWhatThePlayerOwns()
    {
        Harness harness = await Harness.CreateAsync().ConfigureAwait(true);

        await harness.OwnAsync(Sofa).ConfigureAwait(true);

        ImmutableArray<NftCollectionSnapshot> collections = await harness
            .Grain.GetCollectionsForPlayerAsync(Collector, CancellationToken.None)
            .ConfigureAwait(true);

        NftCollectionSnapshot collection = collections.Should().ContainSingle().Which;

        collection.Items.Single(item => item.ProductCode == Sofa).Amount.Should().Be(1);
        collection.Items.Single(item => item.ProductCode == Throne).Amount.Should().Be(0);
        collection.CollectionScore.Should().Be(10, "only the sofa is theirs");
        collection
            .CollectionTotalScore.Should()
            .Be(55, "ten plus forty, plus the five-point boost for finishing");
    }

    [Fact]
    public async Task OwningSeveralOfOneThing_IsNotWorthSeveralTimesTheScore()
    {
        Harness harness = await Harness.CreateAsync().ConfigureAwait(true);

        await harness.OwnAsync(Sofa).ConfigureAwait(true);
        await harness.OwnAsync(Sofa).ConfigureAwait(true);
        await harness.OwnAsync(Sofa).ConfigureAwait(true);

        ImmutableArray<NftCollectionSnapshot> collections = await harness
            .Grain.GetCollectionsForPlayerAsync(Collector, CancellationToken.None)
            .ConfigureAwait(true);

        NftCollectionSnapshot collection = collections.Single();

        collection.Items.Single(item => item.ProductCode == Sofa).Amount.Should().Be(3);
        collection.CollectionScore.Should().Be(10, "a collector scores the item, not the pile");
    }

    [Fact]
    public async Task FinishingACollection_AddsItsBoost()
    {
        Harness harness = await Harness.CreateAsync().ConfigureAwait(true);

        await harness.OwnAsync(Sofa).ConfigureAwait(true);
        await harness.OwnAsync(Throne).ConfigureAwait(true);

        ImmutableArray<NftCollectionSnapshot> collections = await harness
            .Grain.GetCollectionsForPlayerAsync(Collector, CancellationToken.None)
            .ConfigureAwait(true);

        collections.Single().CollectionScore.Should().Be(55, "50 for the pieces, 5 for the set");
    }

    [Fact]
    public async Task TheCollectorLevel_CountsFinishedCollections()
    {
        Harness harness = await Harness.CreateAsync().ConfigureAwait(true);

        CollectorScoreSnapshot before = await harness
            .Grain.GetCollectorScoreAsync(Collector, CancellationToken.None)
            .ConfigureAwait(true);

        before.Level.Should().Be(0);

        await harness.OwnAsync(Sofa).ConfigureAwait(true);
        await harness.OwnAsync(Throne).ConfigureAwait(true);

        CollectorScoreSnapshot after = await harness
            .Grain.GetCollectorScoreAsync(Collector, CancellationToken.None)
            .ConfigureAwait(true);

        after.Level.Should().Be(1);
        after.Score.Should().Be(55);
    }

    [Fact]
    public async Task ABestScore_SurvivesSellingTheFurnitureThatEarnedIt()
    {
        Harness harness = await Harness.CreateAsync().ConfigureAwait(true);

        await harness.OwnAsync(Sofa).ConfigureAwait(true);
        await harness.OwnAsync(Throne).ConfigureAwait(true);

        await harness
            .Grain.GetCollectorScoreAsync(Collector, CancellationToken.None)
            .ConfigureAwait(true);

        await harness.SellEverythingAsync().ConfigureAwait(true);

        CollectorScoreSnapshot after = await harness
            .Grain.GetCollectorScoreAsync(Collector, CancellationToken.None)
            .ConfigureAwait(true);

        after.Score.Should().Be(0, "they own none of it now");
        after.HighestScore.Should().Be(55, "which is not the same as never having owned it");
    }

    [Fact]
    public async Task AHotelWithNoCollections_AnswersAnEmptyShelfRatherThanFailing()
    {
        Harness harness = await Harness.CreateAsync(withCollection: false).ConfigureAwait(true);

        ImmutableArray<NftCollectionSnapshot> collections = await harness
            .Grain.GetCollectionsForPlayerAsync(Collector, CancellationToken.None)
            .ConfigureAwait(true);

        collections.Should().BeEmpty();

        CollectorScoreSnapshot score = await harness
            .Grain.GetCollectorScoreAsync(Collector, CancellationToken.None)
            .ConfigureAwait(true);

        score.Score.Should().Be(0);
        score.Level.Should().Be(0);
    }

    [Fact]
    public async Task AnotherPlayersFurniture_DoesNotCount()
    {
        Harness harness = await Harness.CreateAsync().ConfigureAwait(true);

        await harness.OwnAsync(Sofa, ownerId: 999).ConfigureAwait(true);

        ImmutableArray<NftCollectionSnapshot> collections = await harness
            .Grain.GetCollectionsForPlayerAsync(Collector, CancellationToken.None)
            .ConfigureAwait(true);

        collections.Single().CollectionScore.Should().Be(0);
    }

    private sealed class Harness
    {
        private const string CollectionCode = "summer";

        private readonly DbContextOptions<VortexDbContext> _options;
        private int _nextFurnitureId = 1;

        private Harness(DbContextOptions<VortexDbContext> options, NftCollectionsGrain grain)
        {
            _options = options;
            Grain = grain;
        }

        public NftCollectionsGrain Grain { get; }

        public static async Task<Harness> CreateAsync(bool withCollection = true)
        {
            DbContextOptions<VortexDbContext> options =
                new DbContextOptionsBuilder<VortexDbContext>()
                    .UseInMemoryDatabase($"collections-{Guid.NewGuid():N}")
                    .Options;

            await using (VortexDbContext seed = new(options))
            {
                await seed
                    .FurnitureDefinitions.AddRangeAsync(Definition(1, Sofa), Definition(2, Throne))
                    .ConfigureAwait(true);

                if (withCollection)
                {
                    seed.NftCollections.Add(
                        new NftCollectionEntity
                        {
                            Id = 1,
                            CollectionCode = CollectionCode,
                            Name = "Summer",
                            BoostScore = 5,
                        }
                    );

                    await seed
                        .NftCollectionItems.AddRangeAsync(
                            new NftCollectionItemEntity
                            {
                                Id = 1,
                                NftCollectionEntityId = 1,
                                ProductCode = Sofa,
                                Score = 10,
                            },
                            new NftCollectionItemEntity
                            {
                                Id = 2,
                                NftCollectionEntityId = 1,
                                ProductCode = Throne,
                                Score = 40,
                            }
                        )
                        .ConfigureAwait(true);
                }

                await seed.SaveChangesAsync().ConfigureAwait(true);
            }

            NftCollectionsGrain grain =
                GrainActivationContext.CreateWithIntegerKey<NftCollectionsGrain>(
                    0,
                    new SingleOptionsFactory(options),
                    NullLogger<NftCollectionsGrain>.Instance
                );

            await grain.ReloadAsync(CancellationToken.None).ConfigureAwait(true);

            return new Harness(options, grain);
        }

        /// <summary>Puts one more of a classname in the collector's hands.</summary>
        public async Task OwnAsync(string classname, int ownerId = 101)
        {
            await using VortexDbContext dbCtx = new(_options);

            dbCtx.Furnitures.Add(
                new FurnitureEntity
                {
                    Id = _nextFurnitureId++,
                    PlayerEntityId = ownerId,
                    FurnitureDefinitionEntityId = classname == Sofa ? 1 : 2,
                }
            );

            await dbCtx.SaveChangesAsync().ConfigureAwait(true);
        }

        public async Task SellEverythingAsync()
        {
            await using VortexDbContext dbCtx = new(_options);

            foreach (FurnitureEntity furni in dbCtx.Furnitures)
            {
                furni.DeletedAt = DateTime.UtcNow;
            }

            await dbCtx.SaveChangesAsync().ConfigureAwait(true);
        }

        private static FurnitureDefinitionEntity Definition(int id, string classname) =>
            new()
            {
                Id = id,
                SpriteId = id,
                Name = classname,
                ProductType = ProductType.Floor,
                FurniCategory = FurnitureCategory.Default,
                Logic = "default",
                Width = 1,
                Length = 1,
                StackHeight = 1,
                CanStack = true,
                CanWalk = false,
                CanSit = false,
                CanLay = false,
                CanRecycle = false,
                CanTrade = true,
                CanGroup = true,
                CanSell = true,
            };

        private sealed class SingleOptionsFactory(DbContextOptions<VortexDbContext> options)
            : IDbContextFactory<VortexDbContext>
        {
            public VortexDbContext CreateDbContext() => new(options);
        }
    }
}
