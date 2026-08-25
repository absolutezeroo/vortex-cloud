using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Orleans;
using Orleans.Runtime;
using Vortex.Catalog.Grains;
using Vortex.Database.Context;
using Vortex.Database.Entities.Catalog;
using Vortex.Primitives.Catalog.Grains;
using Vortex.Primitives.Catalog.Snapshots;
using Vortex.Primitives.Events;
using Vortex.Primitives.Inventory.Grains;
using Vortex.Primitives.Players.Enums.Wallet;
using Vortex.Primitives.Players.Grains;
using Vortex.Primitives.Players.Wallet;
using Vortex.Tests.Support;
using Xunit;

namespace Vortex.Database.Tests.Catalog;

/// <summary>
/// A targeted offer is a special price shown to one player, capped at a number of purchases each.
/// The cap is the whole product: past it the offer must cost nothing and grant nothing, and a
/// purchase that fails must not eat one of the player's allowance.
/// </summary>
public sealed class TargetedOfferPurchaseTests
{
    private const int PLAYER_ID = 11;
    private const int OFFER_ID = 300;
    private const int PRICE = 40;
    private const int DEFINITION_ID = 4242;

    [Fact]
    public async Task ThePurchase_ChargesAndGrantsWhatTheOfferNames()
    {
        Harness harness = Harness.Create(limit: 5, perOfferQuantity: 2);

        await harness.PurchaseAsync(units: 3);

        harness.DebitRequests.Single().Amount.Should().Be(PRICE * 3);
        harness.Granted.Should().HaveCount(6, "two per unit, three units");
        harness.Granted.Should().AllBeEquivalentTo(DEFINITION_ID);
        (await harness.PurchaseCountAsync()).Should().Be(3);
    }

    /// <summary>
    /// The requested quantity is wire data. It is clamped to what the player may still buy rather
    /// than trusted -- the plain catalogue purchase learned this the hard way, where an unbounded
    /// quantity wrapped the price negative and handed the goods over free.
    /// </summary>
    [Fact]
    public async Task AQuantityBeyondTheRemainingAllowance_IsClampedToIt()
    {
        Harness harness = Harness.Create(limit: 2, perOfferQuantity: 1);

        await harness.PurchaseAsync(units: 9999);

        harness.DebitRequests.Single().Amount.Should().Be(PRICE * 2);
        harness.Granted.Should().HaveCount(2);
        (await harness.PurchaseCountAsync()).Should().Be(2);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task ANonPositiveQuantity_BuysExactlyOne(int units)
    {
        Harness harness = Harness.Create(limit: 5, perOfferQuantity: 1);

        await harness.PurchaseAsync(units);

        harness.DebitRequests.Single().Amount.Should().Be(PRICE);
        harness.Granted.Should().HaveCount(1);
    }

    [Fact]
    public async Task AnOfferAlreadyAtItsLimit_ChargesNothingAndGrantsNothing()
    {
        Harness harness = Harness.Create(limit: 1, perOfferQuantity: 1);

        await harness.PurchaseAsync(units: 1);
        harness.Reset();

        await harness.PurchaseAsync(units: 1);

        harness.DebitRequests.Should().BeEmpty();
        harness.Granted.Should().BeEmpty();
        (await harness.PurchaseCountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task AnExpiredOffer_ChargesNothingAndGrantsNothing()
    {
        Harness harness = Harness.Create(
            limit: 5,
            perOfferQuantity: 1,
            expiresAt: DateTime.Now.AddMinutes(-1)
        );

        await harness.PurchaseAsync(units: 1);

        harness.DebitRequests.Should().BeEmpty();
        harness.Granted.Should().BeEmpty();
        (await harness.PurchaseCountAsync()).Should().Be(0);
    }

    /// <summary>
    /// The allowance is spent only by a purchase that happened. A player who could not afford the
    /// offer has to still be able to buy it later, and the count is committed after the grant for
    /// exactly that reason.
    /// </summary>
    [Fact]
    public async Task APurchaseTheWalletRefuses_DoesNotSpendTheAllowance()
    {
        Harness harness = Harness.Create(limit: 1, perOfferQuantity: 1);
        harness.DebitSucceeds = false;

        await harness.PurchaseAsync(units: 1);

        harness.Granted.Should().BeEmpty();
        (await harness.PurchaseCountAsync()).Should().Be(0);

        harness.DebitSucceeds = true;
        await harness.PurchaseAsync(units: 1);

        harness.Granted.Should().HaveCount(1);
        (await harness.PurchaseCountAsync()).Should().Be(1);
    }

    /// <summary>An unknown offer is not an error, it is nothing to buy.</summary>
    [Fact]
    public async Task AnUnknownOffer_ChargesNothing()
    {
        Harness harness = Harness.Create(limit: 5, perOfferQuantity: 1);

        TargetedOfferSnapshot? result = await harness.Grain.PurchaseAsync(
            OFFER_ID + 1,
            1,
            CancellationToken.None
        );

        result.Should().BeNull();
        harness.DebitRequests.Should().BeEmpty();
    }

    private sealed class Harness
    {
        private DbContextOptions<VortexDbContext> _options = null!;

        public PlayerTargetedOfferGrain Grain { get; private set; } = null!;

        public bool DebitSucceeds { get; set; } = true;

        public List<WalletDebitRequest> DebitRequests { get; private set; } = [];

        public List<int> Granted { get; private set; } = [];

        public void Reset()
        {
            DebitRequests = [];
            Granted = [];
        }

        public Task<TargetedOfferSnapshot?> PurchaseAsync(int units) =>
            Grain.PurchaseAsync(OFFER_ID, units, CancellationToken.None);

        public async Task<int> PurchaseCountAsync()
        {
            await using VortexDbContext db = new(_options);
            PlayerTargetedOfferEntity? row = await db.PlayerTargetedOffers.FirstOrDefaultAsync(r =>
                r.PlayerEntityId == PLAYER_ID && r.TargetedOfferEntityId == OFFER_ID
            );

            return row?.PurchaseCount ?? 0;
        }

        public static Harness Create(int limit, int perOfferQuantity, DateTime? expiresAt = null)
        {
            Harness h = new()
            {
                _options = new DbContextOptionsBuilder<VortexDbContext>()
                    .UseInMemoryDatabase($"targeted-{Guid.NewGuid():N}")
                    .Options,
            };

            TargetedOfferDefinitionSnapshot definition = new()
            {
                Id = OFFER_ID,
                Identifier = "summer_deal",
                OfferType = 1,
                Title = "Summer",
                Description = string.Empty,
                ImageUrl = string.Empty,
                IconImageUrl = string.Empty,
                ProductCode = "code",
                PriceInCredits = PRICE,
                PriceInActivityPoints = 0,
                ActivityPointType = 0,
                PurchaseLimit = limit,
                ExpiresAt = expiresAt ?? DateTime.Now.AddDays(1),
                SortOrder = 0,
                Products =
                [
                    new TargetedOfferProductSnapshot
                    {
                        ProductCode = "code",
                        FurnitureDefinitionId = DEFINITION_ID,
                        Quantity = perOfferQuantity,
                    },
                ],
            };

            h.Grain = new PlayerTargetedOfferGrain(
                h.BuildGrainFactory(definition),
                new TestDbContextFactory(h._options),
                FakeProxy.Create<IEventPublisher>(_ => Task.CompletedTask),
                new Commerce.NullCommerceJournal(),
                NullLogger<PlayerTargetedOfferGrain>.Instance
            );

            GrainId grainId = GrainId.Create(
                GrainType.Create("playertargetedoffer"),
                GrainIdKeyExtensions.CreateIntegerKey(PLAYER_ID)
            );
            IGrainContext context = FakeProxy.Create<IGrainContext>(call =>
                call.Method.Name == $"get_{nameof(IGrainContext.GrainId)}" ? grainId : null
            );
            FieldInfo field =
                typeof(Grain).GetField(
                    "<GrainContext>k__BackingField",
                    BindingFlags.Instance | BindingFlags.NonPublic
                ) ?? throw new InvalidOperationException("Grain.GrainContext backing field moved.");
            field.SetValue(h.Grain, context);

            return h;
        }

        private IGrainFactory BuildGrainFactory(TargetedOfferDefinitionSnapshot definition)
        {
            IPlayerWalletGrain wallet = FakeProxy.Create<IPlayerWalletGrain>(call =>
            {
                switch (call.Method.Name)
                {
                    case nameof(IPlayerWalletGrain.TryDebitAsync):
                        if (!DebitSucceeds)
                        {
                            return Task.FromResult(
                                WalletDebitResult.InsufficientBalance(
                                    new WalletDebitFailure
                                    {
                                        CurrencyKind = new CurrencyKind
                                        {
                                            CurrencyType = CurrencyType.Credits,
                                        },
                                        Amount = PRICE,
                                    }
                                )
                            );
                        }

                        DebitRequests.AddRange((List<WalletDebitRequest>)call.Args![0]!);

                        return Task.FromResult(WalletDebitResult.Success());

                    default:
                        return null;
                }
            });

            IInventoryGrain inventory = FakeProxy.Create<IInventoryGrain>(call =>
            {
                // The grant asks for every copy in one call now — one commit rather than one per
                // copy — so what lands here is a definition and a count.
                if (call.Method.Name == nameof(IInventoryGrain.GrantFurnitureDefinitionCopiesAsync))
                {
                    for (int i = 0; i < (int)call.Args![2]!; i++)
                    {
                        Granted.Add((int)call.Args![0]!);
                    }
                }
                else if (call.Method.Name == nameof(IInventoryGrain.GrantFurnitureDefinitionAsync))
                {
                    Granted.Add((int)call.Args![0]!);
                }

                return Task.CompletedTask;
            });

            ITargetedOfferManagerGrain manager = FakeProxy.Create<ITargetedOfferManagerGrain>(
                call =>
                    call.Method.Name == nameof(ITargetedOfferManagerGrain.GetDefinitionsAsync)
                        ? Task.FromResult(ImmutableArray.Create(definition))
                        : null
            );

            return FakeProxy.Create<IGrainFactory>(call =>
                call.Method.IsGenericMethod
                    ? call.Method.GetGenericArguments()[0] switch
                    {
                        Type t when t == typeof(IPlayerWalletGrain) => wallet,
                        Type t when t == typeof(IInventoryGrain) => inventory,
                        Type t when t == typeof(ITargetedOfferManagerGrain) => manager,
                        _ => null,
                    }
                    : null
            );
        }

        private sealed class TestDbContextFactory(DbContextOptions<VortexDbContext> options)
            : IDbContextFactory<VortexDbContext>
        {
            public VortexDbContext CreateDbContext() => new(options);
        }
    }
}
