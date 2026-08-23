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
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Inventory.Grains;
using Vortex.Tests.Support;
using Xunit;

namespace Vortex.Players.Tests.Collectibles;

/// <summary>
///     The Relics tab. The client has one button and it claims everything at once, so the rule that
///     matters is that a second press hands nothing over again.
/// </summary>
public sealed class PlayerNftClaimsGrainTests
{
    private const int PlayerId = 42;
    private const string Lamp = "02_dragonlamp_skream";

    [Fact]
    public async Task AnOutstandingClaim_IsListedWithTheSpriteIdAndTheWalletEchoedBack()
    {
        Harness harness = await Harness.CreateAsync().ConfigureAwait(true);

        ImmutableArray<NftClaimSnapshot> claims = await harness
            .Grain.GetClaimsAsync("0xwallet", CancellationToken.None)
            .ConfigureAwait(true);

        NftClaimSnapshot claim = claims.Should().ContainSingle().Which;

        claim.ProductCode.Should().Be(Lamp);
        claim.Wallet.Should().Be("0xwallet", "the client shows the wallet under the reward");

        // Same trap as everywhere else in this domain: the client reads the item type with parseInt.
        claim.ClaimItem.Product.ItemTypeId.Should().Be("77");
        claim.ClaimItem.Product.ItemTypeId.Should().NotBe(Lamp);
        claim.ClaimItem.Product.ProductTypeId.Should().Be(CollectibleProductIdentity.Floor);
    }

    [Fact]
    public async Task ClaimingTwice_HandsOverNothingTheSecondTime()
    {
        Harness harness = await Harness.CreateAsync(claimLimit: 2).ConfigureAwait(true);

        (await harness.Grain.ClaimAllAsync(CancellationToken.None).ConfigureAwait(true))
            .Should()
            .Be(2, "both copies are owed");

        (await harness.Grain.ClaimAllAsync(CancellationToken.None).ConfigureAwait(true))
            .Should()
            .Be(0, "the entitlement is spent");

        ImmutableArray<NftClaimSnapshot> left = await harness
            .Grain.GetClaimsAsync("0xwallet", CancellationToken.None)
            .ConfigureAwait(true);

        left.Should().BeEmpty("a spent claim is history, not a reward");
    }

    [Fact]
    public async Task AnExpiredClaim_IsNeitherListedNorClaimable()
    {
        Harness harness = await Harness
            .CreateAsync(validTo: DateTime.UtcNow.AddDays(-1))
            .ConfigureAwait(true);

        (
            await harness
                .Grain.GetClaimsAsync("0xwallet", CancellationToken.None)
                .ConfigureAwait(true)
        )
            .Should()
            .BeEmpty();
        (await harness.Grain.ClaimAllAsync(CancellationToken.None).ConfigureAwait(true))
            .Should()
            .Be(0);
    }

    /// <summary>
    ///     A claim naming furniture that does not exist must not consume itself: it would take the
    ///     entitlement away and hand over nothing, which is unrecoverable for the player.
    /// </summary>
    [Fact]
    public async Task AClaimNamingUnknownFurniture_IsLeftOutstanding()
    {
        Harness harness = await Harness
            .CreateAsync(productCode: "does_not_exist")
            .ConfigureAwait(true);

        (await harness.Grain.ClaimAllAsync(CancellationToken.None).ConfigureAwait(true))
            .Should()
            .Be(0);

        (
            await harness
                .Grain.GetClaimsAsync("0xwallet", CancellationToken.None)
                .ConfigureAwait(true)
        )
            .Should()
            .ContainSingle("it stays for an admin to fix");
    }

    private sealed class Harness
    {
        private Harness(PlayerNftClaimsGrain grain) => Grain = grain;

        public PlayerNftClaimsGrain Grain { get; }

        public static async Task<Harness> CreateAsync(
            string productCode = Lamp,
            int claimLimit = 1,
            DateTime? validTo = null
        )
        {
            DbContextOptions<VortexDbContext> options =
                new DbContextOptionsBuilder<VortexDbContext>()
                    .UseInMemoryDatabase($"claims-{Guid.NewGuid():N}")
                    .Options;

            await using (VortexDbContext seed = new(options))
            {
                seed.FurnitureDefinitions.Add(
                    new FurnitureDefinitionEntity
                    {
                        Id = 77,
                        SpriteId = 77,
                        Name = Lamp,
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
                    }
                );

                seed.NftClaims.Add(
                    new NftClaimEntity
                    {
                        Id = 1,
                        PlayerEntityId = PlayerId,
                        ClaimCode = "c1",
                        ProductCode = productCode,
                        ClaimLimit = claimLimit,
                        ValidTo = validTo,
                    }
                );

                await seed.SaveChangesAsync().ConfigureAwait(true);
            }

            // Only the inventory is reached, and only to be handed a definition id.
            IInventoryGrain inventory = FakeProxy.Create<IInventoryGrain>(_ => Task.CompletedTask);

            IGrainFactory grainFactory = FakeProxy.Create<IGrainFactory>(call =>
                call.Method.IsGenericMethod
                && call.Method.GetGenericArguments()[0] == typeof(IInventoryGrain)
                    ? inventory
                    : null
            );

            PlayerNftClaimsGrain grain =
                GrainActivationContext.CreateWithIntegerKey<PlayerNftClaimsGrain>(
                    PlayerId,
                    new SingleOptionsFactory(options),
                    grainFactory,
                    NullLogger<PlayerNftClaimsGrain>.Instance
                );

            return new Harness(grain);
        }

        private sealed class SingleOptionsFactory(DbContextOptions<VortexDbContext> options)
            : IDbContextFactory<VortexDbContext>
        {
            public VortexDbContext CreateDbContext() => new(options);
        }
    }
}
