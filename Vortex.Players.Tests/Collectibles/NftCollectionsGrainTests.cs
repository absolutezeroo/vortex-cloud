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

    /// <summary>
    ///     The client parses a collection's status and never reads it, so withholding an
    ///     unpublished collection is something only the server can do. Without this, "Draft" would
    ///     be a word in the admin panel and nothing else.
    /// </summary>
    [Theory]
    [InlineData(NftCollectionStatus.Draft)]
    [InlineData(NftCollectionStatus.Archived)]
    public async Task ACollectionPlayersShouldNotSee_IsNotSent(int status)
    {
        Harness harness = await Harness.CreateAsync(status: status).ConfigureAwait(true);

        await harness.OwnAsync(Sofa).ConfigureAwait(true);

        ImmutableArray<NftCollectionSnapshot> collections = await harness
            .Grain.GetCollectionsForPlayerAsync(Collector, CancellationToken.None)
            .ConfigureAwait(true);

        collections.Should().BeEmpty();
    }

    /// <summary>
    ///     The client draws a collectible by reading <c>itemTypeId</c> with <c>parseInt</c> and
    ///     looking that number up in its own furniture tables, so it has to be the sprite id. Sending
    ///     the classname is not a visible failure — it draws whatever sprite the leading digits
    ///     happen to name, which is how a dragon lamp came out as a post-it.
    /// </summary>
    [Fact]
    public async Task AnItemIsIdentifiedByItsSpriteId_NotItsClassname()
    {
        Harness harness = await Harness.CreateAsync().ConfigureAwait(true);

        ImmutableArray<NftCollectionSnapshot> collections = await harness
            .Grain.GetCollectionsForPlayerAsync(Collector, CancellationToken.None)
            .ConfigureAwait(true);

        CollectibleProductItemSnapshot sofa = collections
            .Single()
            .Items.Single(item => item.ProductCode == Sofa);

        // The harness gives club_sofa sprite id 1 and makes it a floor item.
        sofa.ItemTypeId.Should().Be("1");
        sofa.ItemTypeId.Should().NotBe(Sofa);
        sofa.ProductTypeId.Should().Be(CollectibleProductIdentity.Floor);
    }

    /// <summary>
    ///     A classname is not a key. The client's own furnidata ships duplicates —
    ///     <c>clothing_nftshoulderdragon1</c> is two entries there — so the mirror table holds two
    ///     live rows under one name, and keying a dictionary on it threw. The grain catches on load
    ///     and empties its cache, so one duplicated code blanked the whole collectibles hub for
    ///     every player: "Failed to load collectible collections", nothing shown.
    /// </summary>
    [Fact]
    public async Task ACollectionSurvivesAClassnameThatNamesTwoDefinitions()
    {
        Harness harness = await Harness
            .CreateAsync(withDuplicateSofaDefinition: true)
            .ConfigureAwait(true);

        ImmutableArray<NftCollectionSnapshot> collections = await harness
            .Grain.GetCollectionsForPlayerAsync(Collector, CancellationToken.None)
            .ConfigureAwait(true);

        collections.Should().ContainSingle();
        collections.Single().Items.Should().HaveCount(2);
    }

    /// <summary>
    ///     Which of the duplicates wins matters less than it being the same one on every reload — an
    ///     item whose icon changes between restarts is worse than one drawn from the older row.
    /// </summary>
    [Fact]
    public async Task ADuplicatedClassnameResolvesToItsLowestIdDefinition()
    {
        Harness harness = await Harness
            .CreateAsync(withDuplicateSofaDefinition: true)
            .ConfigureAwait(true);

        ImmutableArray<NftCollectionSnapshot> collections = await harness
            .Grain.GetCollectionsForPlayerAsync(Collector, CancellationToken.None)
            .ConfigureAwait(true);

        CollectibleProductItemSnapshot sofa = collections
            .Single()
            .Items.Single(item => item.ProductCode == Sofa);

        // Definitions 1 and 99 both answer to club_sofa; the harness gives each sprite id == id.
        sofa.ItemTypeId.Should().Be("1");
    }

    /// <summary>
    ///     The two prizes go through the same resolution as the items. They used to be built with a
    ///     hardcoded product type and the classname as the item type, so every prize in every
    ///     collection was drawn from the wrong table with a nonsense id.
    /// </summary>
    [Fact]
    public async Task APrizeIsResolvedTheSameWayAnItemIs()
    {
        Harness harness = await Harness.CreateAsync(rewardProductCode: Throne).ConfigureAwait(true);

        ImmutableArray<NftCollectionSnapshot> collections = await harness
            .Grain.GetCollectionsForPlayerAsync(Collector, CancellationToken.None)
            .ConfigureAwait(true);

        CollectibleProductItemSnapshot reward = collections.Single().RewardItem!;

        reward.ProductCode.Should().Be(Throne);
        reward.ItemTypeId.Should().Be("2", "the harness gives throne sprite id 2");
        reward.ProductTypeId.Should().Be(CollectibleProductIdentity.Floor);
    }

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

    /// <summary>
    ///     Converting a collectible into a Relic destroys the furniture. If only furniture counted,
    ///     minting would lower the score of the collection the item belongs to — the Collectors
    ///     Guild punishing collecting — and a finished set would come apart the moment its owner
    ///     used the tab beside it.
    /// </summary>
    [Fact]
    public async Task ARelic_CountsTowardsItsCollectionLikeTheFurnitureItWas()
    {
        Harness harness = await Harness.CreateAsync().ConfigureAwait(true);

        await harness.OwnAsync(Sofa).ConfigureAwait(true);
        await harness.OwnRelicAsync(Throne).ConfigureAwait(true);

        CollectorScoreSnapshot score = await harness
            .Grain.GetCollectorScoreAsync(Collector, CancellationToken.None)
            .ConfigureAwait(true);

        // One owned, one converted: the set is still finished, boost included.
        score.Score.Should().Be(55);
        score.Level.Should().Be(1);
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

        public static async Task<Harness> CreateAsync(
            bool withCollection = true,
            int status = NftCollectionStatus.Visible,
            string? rewardProductCode = null,
            bool withDuplicateSofaDefinition = false
        )
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

                if (withDuplicateSofaDefinition)
                {
                    // A second live definition under the same classname, exactly as furnidata ships
                    // it. Higher id, so the lowest-id rule has something to reject.
                    await seed
                        .FurnitureDefinitions.AddAsync(Definition(99, Sofa))
                        .ConfigureAwait(true);
                }

                if (withCollection)
                {
                    seed.NftCollections.Add(
                        new NftCollectionEntity
                        {
                            Id = 1,
                            CollectionCode = CollectionCode,
                            Name = "Summer",
                            BoostScore = 5,
                            // Spelled out rather than left to default: the default is Draft, which
                            // players never see, so every other test here depends on this.
                            Status = status,
                            RewardProductCode = rewardProductCode,
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

        /// <summary>Turns one of a classname into a Relic — the furniture is gone, the asset is not.</summary>
        public async Task OwnRelicAsync(string classname, int ownerId = 101)
        {
            await using VortexDbContext dbCtx = new(_options);

            dbCtx.NftAssets.Add(
                new NftAssetEntity
                {
                    PlayerEntityId = ownerId,
                    ProductCode = classname,
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
