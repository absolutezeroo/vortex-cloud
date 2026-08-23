using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Orleans;
using Vortex.Collectibles.Grains;
using Vortex.Database.Context;
using Vortex.Database.Entities.Collectibles;
using Vortex.Database.Entities.Furniture;
using Vortex.Players.Grains;
using Vortex.Primitives.Collectibles;
using Vortex.Primitives.Collectibles.Grains;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Tests.Support;
using Xunit;

namespace Vortex.Players.Tests.Collectibles;

/// <summary>
/// What may be converted into a Relic. The rules worth pinning: a closed window is not sent at all,
/// the client is told the furniture's sprite id rather than its classname, and floor and wall are
/// numbered the opposite way round from the product struct sent beside them.
/// </summary>
public sealed class NftMintingGrainTests
{
    private const string Sofa = "nft_sofa";
    private const string Poster = "nft_poster";
    private const string Ghost = "nft_nothing";

    [Fact]
    public async Task AnOpenType_IsListedWithItsSpriteIdAndItsKind()
    {
        Harness harness = await Harness.CreateAsync().ConfigureAwait(true);

        ImmutableArray<MintableItemTypeSnapshot> types = await harness
            .Grain.GetMintableItemTypesAsync(CancellationToken.None)
            .ConfigureAwait(true);

        MintableItemTypeSnapshot sofa = types.Single(type => type.Price == 5);

        // The harness gives nft_sofa sprite id 1 and makes it a floor item.
        sofa.ItemTypeId.Should().Be(1);
        sofa.ItemType.Should().Be(MintableItemKind.Floor);
    }

    /// <summary>
    ///     A wall item is kind 1 here, where the product struct beside it calls wall 0. Sending
    ///     either number in the other message's meaning makes the client search the wrong
    ///     inventory, find nothing, and leave the convert button grey with no reason on screen.
    /// </summary>
    [Fact]
    public async Task AWallItem_IsKindOne_NotZero()
    {
        Harness harness = await Harness.CreateAsync().ConfigureAwait(true);

        ImmutableArray<MintableItemTypeSnapshot> types = await harness
            .Grain.GetMintableItemTypesAsync(CancellationToken.None)
            .ConfigureAwait(true);

        MintableItemTypeSnapshot poster = types.Single(type => type.ItemTypeId == 2);

        poster.ItemType.Should().Be(MintableItemKind.Wall);
        poster.ItemType.Should().NotBe(CollectibleProductIdentity.Wall);
    }

    /// <summary>
    ///     The client disables the convert button once the end time has passed and says nothing
    ///     about why, so a closed window is filtered out here rather than sent and greyed out.
    /// </summary>
    [Fact]
    public async Task AClosedWindow_IsNotOfferedAtAll()
    {
        Harness harness = await Harness
            .CreateAsync(sofaEndsAt: DateTime.UtcNow.AddHours(-1))
            .ConfigureAwait(true);

        ImmutableArray<MintableItemTypeSnapshot> types = await harness
            .Grain.GetMintableItemTypesAsync(CancellationToken.None)
            .ConfigureAwait(true);

        types.Should().NotContain(type => type.ItemTypeId == 1);

        MintableTypeTerms? terms = await harness
            .Grain.FindMintableTermsAsync(Sofa, CancellationToken.None)
            .ConfigureAwait(true);

        // And the conversion itself is refused on the same rule, not just hidden from the list.
        terms.Should().BeNull();
    }

    [Fact]
    public async Task AWindowThatHasNotOpened_IsNotOfferedEither()
    {
        Harness harness = await Harness
            .CreateAsync(sofaStartsAt: DateTime.UtcNow.AddDays(1))
            .ConfigureAwait(true);

        MintableTypeTerms? terms = await harness
            .Grain.FindMintableTermsAsync(Sofa, CancellationToken.None)
            .ConfigureAwait(true);

        terms.Should().BeNull();
    }

    /// <summary>
    ///     A type naming furniture this hotel does not ship would be drawn with sprite id 0 and
    ///     match nothing in the player's inventory — a row that can never be used, with no
    ///     explanation anywhere.
    /// </summary>
    [Fact]
    public async Task ATypeNamingNoFurniture_IsLeftOut()
    {
        Harness harness = await Harness.CreateAsync(withGhost: true).ConfigureAwait(true);

        ImmutableArray<MintableItemTypeSnapshot> types = await harness
            .Grain.GetMintableItemTypesAsync(CancellationToken.None)
            .ConfigureAwait(true);

        types.Should().HaveCount(2);

        MintableTypeTerms? terms = await harness
            .Grain.FindMintableTermsAsync(Ghost, CancellationToken.None)
            .ConfigureAwait(true);

        terms.Should().BeNull();
    }

    /// <summary>
    ///     The price a conversion is charged at comes from here, never from the client: the mint
    ///     message carries an item id and a wallet, and nothing else.
    /// </summary>
    [Fact]
    public async Task TheTerms_CarryThePublishedPrice()
    {
        Harness harness = await Harness.CreateAsync().ConfigureAwait(true);

        MintableTypeTerms? terms = await harness
            .Grain.FindMintableTermsAsync(Sofa, CancellationToken.None)
            .ConfigureAwait(true);

        terms.Should().NotBeNull();
        terms!.StampPrice.Should().Be(5);
    }

    [Fact]
    public async Task ADisabledBundle_IsNotOnSale()
    {
        Harness harness = await Harness.CreateAsync().ConfigureAwait(true);

        ImmutableArray<MintTokenOfferSnapshot> offers = await harness
            .Grain.GetTokenOffersAsync(CancellationToken.None)
            .ConfigureAwait(true);

        offers.Should().ContainSingle();
        offers.Single().AmountTokens.Should().Be(10);

        (await harness.Grain.FindTokenOfferAsync(2, CancellationToken.None).ConfigureAwait(true))
            .Should()
            .BeNull("the second bundle is disabled");
    }

    private sealed class Harness
    {
        private Harness(NftMintingGrain grain) => Grain = grain;

        public NftMintingGrain Grain { get; }

        public static async Task<Harness> CreateAsync(
            DateTime? sofaStartsAt = null,
            DateTime? sofaEndsAt = null,
            bool withGhost = false
        )
        {
            DbContextOptions<VortexDbContext> options =
                new DbContextOptionsBuilder<VortexDbContext>()
                    .UseInMemoryDatabase($"minting-{Guid.NewGuid():N}")
                    .Options;

            await using (VortexDbContext seed = new(options))
            {
                await seed
                    .FurnitureDefinitions.AddRangeAsync(
                        Definition(1, Sofa, ProductType.Floor),
                        Definition(2, Poster, ProductType.Wall)
                    )
                    .ConfigureAwait(true);

                await seed
                    .NftMintableItemTypes.AddRangeAsync(
                        Type(1, Sofa, 5, sofaStartsAt, sofaEndsAt),
                        Type(2, Poster, 3, null, null)
                    )
                    .ConfigureAwait(true);

                if (withGhost)
                {
                    seed.NftMintableItemTypes.Add(Type(3, Ghost, 1, null, null));
                }

                await seed
                    .NftMintTokenOffers.AddRangeAsync(
                        new NftMintTokenOfferEntity
                        {
                            Id = 1,
                            ProductCode = "stamps_10",
                            SilverPrice = 100,
                            AmountTokens = 10,
                        },
                        new NftMintTokenOfferEntity
                        {
                            Id = 2,
                            ProductCode = "stamps_50",
                            SilverPrice = 400,
                            AmountTokens = 50,
                            Enabled = false,
                        }
                    )
                    .ConfigureAwait(true);

                await seed.SaveChangesAsync().ConfigureAwait(true);
            }

            // Nothing here reaches another grain: the hotel-wide switch is the only call that does,
            // and it is a config read rather than part of what the list means.
            IGrainFactory grainFactory = FakeProxy.Create<IGrainFactory>(_ => null);

            NftMintingGrain grain = GrainActivationContext.CreateWithIntegerKey<NftMintingGrain>(
                0,
                new SingleOptionsFactory(options),
                grainFactory,
                NullLogger<NftMintingGrain>.Instance
            );

            await grain.ReloadAsync(CancellationToken.None).ConfigureAwait(true);

            return new Harness(grain);
        }

        private static NftMintableItemTypeEntity Type(
            int id,
            string classname,
            int price,
            DateTime? startsAt,
            DateTime? endsAt
        ) =>
            new()
            {
                Id = id,
                ProductCode = classname,
                StampPrice = price,
                StartsAt = startsAt ?? DateTime.UtcNow.AddDays(-1),
                EndsAt = endsAt ?? DateTime.UtcNow.AddDays(30),
            };

        private static FurnitureDefinitionEntity Definition(
            int id,
            string classname,
            ProductType productType
        ) =>
            new()
            {
                Id = id,
                SpriteId = id,
                Name = classname,
                ProductType = productType,
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
