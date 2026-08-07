using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Orleans;
using Orleans.Runtime;
using Vortex.Catalog.Exceptions;
using Vortex.Catalog.Grains;
using Vortex.Primitives.Catalog;
using Vortex.Primitives.Catalog.Enums;
using Vortex.Primitives.Catalog.Snapshots;
using Vortex.Primitives.Events;
using Vortex.Primitives.Inventory.Grains;
using Vortex.Primitives.Orleans.Snapshots.Players;
using Vortex.Primitives.Players;
using Vortex.Primitives.Players.Enums.Wallet;
using Vortex.Primitives.Players.Grains;
using Vortex.Primitives.Players.Wallet;
using Vortex.Primitives.Rooms;
using Vortex.Tests.Support;
using Xunit;

namespace Vortex.Database.Tests.Catalog;

/// <summary>
/// Gifting used to hand-roll its own debit-then-grant in the packet handler, which skipped the club
/// gate, the HC discount, the credit-spend tracking, the audit events and the refund. These lock the
/// gift path onto the same guarantees as a normal purchase.
/// </summary>
public sealed class CatalogGiftPurchaseTests
{
    private const int BUYER_ID = 11;
    private const int RECEIVER_ID = 22;
    private const int OFFER_ID = 900;

    /// <summary>
    /// Stuff type 0 names no <c>present_gen*</c> furniture, so wrapping cannot be built and the
    /// purchase falls back to granting the offer outright. That is deliberate here: these tests are
    /// about the economy guarantees — club gate, discount, spend tracking, refund — and the fallback
    /// keeps them asserting on the recipient's inventory rather than on the contents of a box.
    /// </summary>
    private static readonly GiftWrappingSpec NoWrapping = new()
    {
        StuffTypeId = 0,
        BoxTypeId = 0,
        RibbonTypeId = 0,
        Message = string.Empty,
        ShowPurchaserName = false,
    };

    [Fact]
    public async Task PurchaseOfferAsGiftAsync_GrantsToReceiverAndChargesBuyer()
    {
        Harness harness = new Harness(NewOffer(costCredits: 50));

        CatalogOfferSnapshot offer = await harness
            .Grain.PurchaseOfferAsGiftAsync(
                CatalogType.Normal,
                OFFER_ID,
                string.Empty,
                new PlayerId(RECEIVER_ID),
                NoWrapping,
                CancellationToken.None
            )
            .ConfigureAwait(true);

        offer.Id.Should().Be(OFFER_ID);
        harness.GrantedToPlayerIds.Should().Equal(RECEIVER_ID);
        harness.DebitedPlayerIds.Should().Equal(BUYER_ID);
        harness.DebitRequests.Single().Amount.Should().Be(50);
        harness.TrackedCreditSpend.Should().Equal(50);
        harness.CreditBackCalls.Should().Be(0);
    }

    [Fact]
    public async Task PurchaseOfferAsGiftAsync_ClubOnlyOffer_NonMemberIsRefusedBeforeAnyDebit()
    {
        Harness harness = new Harness(NewOffer(costCredits: 50, clubLevel: 1));

        Func<Task> act = () =>
            harness.Grain.PurchaseOfferAsGiftAsync(
                CatalogType.Normal,
                OFFER_ID,
                string.Empty,
                new PlayerId(RECEIVER_ID),
                NoWrapping,
                CancellationToken.None
            );

        CatalogPurchaseException ex = (
            await act.Should().ThrowAsync<CatalogPurchaseException>().ConfigureAwait(true)
        ).Which;

        ex.ErrorType.Should().Be(CatalogPurchaseErrorType.RequiresHabboClub);
        harness.DebitRequests.Should().BeEmpty();
        harness.GrantedToPlayerIds.Should().BeEmpty();
    }

    [Fact]
    public async Task PurchaseOfferAsGiftAsync_ClubMember_GetsTheOfferDiscount()
    {
        Harness harness = new Harness(NewOffer(costCredits: 100, discountPercent: 25))
        {
            Club = new ClubSubscriptionSnapshot { IsActive = true, IsVip = false },
        };

        await harness
            .Grain.PurchaseOfferAsGiftAsync(
                CatalogType.Normal,
                OFFER_ID,
                string.Empty,
                new PlayerId(RECEIVER_ID),
                NoWrapping,
                CancellationToken.None
            )
            .ConfigureAwait(true);

        harness.DebitRequests.Single().Amount.Should().Be(75);
        harness.TrackedCreditSpend.Should().Equal(75);
    }

    [Fact]
    public async Task PurchaseOfferAsGiftAsync_NonGiftableOffer_IsRefused()
    {
        Harness harness = new Harness(NewOffer(costCredits: 50, canGift: false));

        Func<Task> act = () =>
            harness.Grain.PurchaseOfferAsGiftAsync(
                CatalogType.Normal,
                OFFER_ID,
                string.Empty,
                new PlayerId(RECEIVER_ID),
                NoWrapping,
                CancellationToken.None
            );

        await act.Should().ThrowAsync<CatalogPurchaseException>().ConfigureAwait(true);
        harness.DebitRequests.Should().BeEmpty();
    }

    [Fact]
    public async Task PurchaseOfferAsGiftAsync_GrantFails_RefundsTheBuyer()
    {
        Harness harness = new Harness(NewOffer(costCredits: 50)) { GrantThrows = true };

        Func<Task> act = () =>
            harness.Grain.PurchaseOfferAsGiftAsync(
                CatalogType.Normal,
                OFFER_ID,
                string.Empty,
                new PlayerId(RECEIVER_ID),
                NoWrapping,
                CancellationToken.None
            );

        await act.Should().ThrowAsync<InvalidOperationException>().ConfigureAwait(true);
        harness.CreditBackCalls.Should().Be(1);
    }

    [Fact]
    public async Task PurchaseOfferAsGiftAsync_PublishesSpendAndRecipientEvents()
    {
        Harness harness = new Harness(NewOffer(costCredits: 50));

        await harness
            .Grain.PurchaseOfferAsGiftAsync(
                CatalogType.Normal,
                OFFER_ID,
                string.Empty,
                new PlayerId(RECEIVER_ID),
                NoWrapping,
                CancellationToken.None
            )
            .ConfigureAwait(true);

        harness.Events.OfType<CatalogPurchasedEvent>().Single().PlayerId.Should().Be(BUYER_ID);

        CatalogGiftPurchasedEvent gift = harness
            .Events.OfType<CatalogGiftPurchasedEvent>()
            .Single();
        gift.BuyerPlayerId.Should().Be(BUYER_ID);
        gift.ReceiverPlayerId.Should().Be(RECEIVER_ID);
        gift.OfferId.Should().Be(OFFER_ID);
        gift.CreditCost.Should().Be(50);
    }

    [Fact]
    public async Task PurchaseOfferAsGiftAsync_InsufficientBalance_NeverGrants()
    {
        Harness harness = new Harness(NewOffer(costCredits: 50)) { DebitSucceeds = false };

        Func<Task> act = () =>
            harness.Grain.PurchaseOfferAsGiftAsync(
                CatalogType.Normal,
                OFFER_ID,
                string.Empty,
                new PlayerId(RECEIVER_ID),
                NoWrapping,
                CancellationToken.None
            );

        CatalogPurchaseException ex = (
            await act.Should().ThrowAsync<CatalogPurchaseException>().ConfigureAwait(true)
        ).Which;

        ex.ErrorType.Should().Be(CatalogPurchaseErrorType.NotEnoughCredits);
        harness.GrantedToPlayerIds.Should().BeEmpty();
    }

    private static CatalogOfferSnapshot NewOffer(
        int costCredits,
        int clubLevel = 0,
        int discountPercent = 0,
        bool canGift = true
    ) =>
        new()
        {
            Id = OFFER_ID,
            PageId = 1,
            LocalizationId = "offer",
            Rentable = false,
            CostCredits = costCredits,
            CostCurrency = 0,
            CurrencyTypeId = null,
            CostSilver = 0,
            CanGift = canGift,
            CanBundle = false,
            ClubLevel = clubLevel,
            Visible = true,
            ProductIds = [],
            Products = [],
            DiscountPercent = discountPercent,
        };

    /// <summary>
    /// Builds a <see cref="CatalogPurchaseGrain"/> outside a silo. The grain reads its own primary
    /// key, so the activation context is stubbed rather than left null; every collaborator is a
    /// recording proxy so the assertions can be about who was charged and who was granted.
    /// </summary>
    private sealed class Harness
    {
        public Harness(CatalogOfferSnapshot offer)
        {
            Offer = offer;
            Grain = BuildGrain();
        }

        public CatalogOfferSnapshot Offer { get; }

        public CatalogPurchaseGrain Grain { get; }

        public ClubSubscriptionSnapshot Club { get; set; } =
            new ClubSubscriptionSnapshot { IsActive = false, IsVip = false };

        public bool GrantThrows { get; set; }

        public bool DebitSucceeds { get; set; } = true;

        public List<int> GrantedToPlayerIds { get; } = [];

        public List<int> DebitedPlayerIds { get; } = [];

        public List<WalletDebitRequest> DebitRequests { get; } = [];

        public List<int> TrackedCreditSpend { get; } = [];

        public List<IEvent> Events { get; } = [];

        public int CreditBackCalls { get; private set; }

        private CatalogPurchaseGrain BuildGrain()
        {
            CatalogPurchaseGrain grain = new CatalogPurchaseGrain(
                BuildGrainFactory(),
                new StubCatalogService(Offer),
                new RecordingEventPublisher(Events),
                FakeProxy.Create<IRoomAdvertisementService>(_ => null),
                NullLogger<CatalogPurchaseGrain>.Instance
            );

            SetGrainContext(grain, BUYER_ID);

            return grain;
        }

        private IGrainFactory BuildGrainFactory()
        {
            IPlayerWalletGrain wallet = FakeProxy.Create<IPlayerWalletGrain>(call =>
            {
                switch (call.Method.Name)
                {
                    case nameof(IPlayerWalletGrain.TryDebitAsync):
                        DebitedPlayerIds.Add(BUYER_ID);

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
                                        Amount = 50,
                                    }
                                )
                            );
                        }

                        DebitRequests.AddRange((List<WalletDebitRequest>)call.Args![0]!);

                        return Task.FromResult(WalletDebitResult.Success());

                    case nameof(IPlayerWalletGrain.CreditBackAsync):
                        CreditBackCalls++;

                        return Task.CompletedTask;

                    default:
                        return null;
                }
            });

            IPlayerGrain player = FakeProxy.Create<IPlayerGrain>(call =>
                call.Method.Name switch
                {
                    nameof(IPlayerGrain.GetClubSubscriptionAsync) => Task.FromResult(Club),
                    nameof(IPlayerGrain.TrackCreditSpendAsync) => TrackAsync((int)call.Args![0]!),
                    _ => null,
                }
            );

            IInventoryGrain inventory = FakeProxy.Create<IInventoryGrain>(call =>
            {
                if (call.Method.Name != nameof(IInventoryGrain.GrantCatalogOfferAsync))
                {
                    return null;
                }

                if (GrantThrows)
                {
                    throw new InvalidOperationException("grant failed");
                }

                GrantedToPlayerIds.Add(RECEIVER_ID);

                return Task.CompletedTask;
            });

            return FakeProxy.Create<IGrainFactory>(call =>
                call.Method.IsGenericMethod
                    ? call.Method.GetGenericArguments()[0] switch
                    {
                        Type t when t == typeof(IPlayerWalletGrain) => wallet,
                        Type t when t == typeof(IPlayerGrain) => player,
                        Type t when t == typeof(IInventoryGrain) => inventory,
                        _ => null,
                    }
                    : null
            );
        }

        private Task TrackAsync(int credits)
        {
            TrackedCreditSpend.Add(credits);

            return Task.CompletedTask;
        }

        /// <summary>The grain calls <c>this.GetPrimaryKeyLong()</c>, which resolves through the
        /// activation context Orleans would normally install.</summary>
        private static void SetGrainContext(Grain grain, int playerId)
        {
            GrainId grainId = GrainId.Create(
                GrainType.Create("catalogpurchase"),
                GrainIdKeyExtensions.CreateIntegerKey(playerId)
            );

            IGrainContext context = FakeProxy.Create<IGrainContext>(call =>
                call.Method.Name == $"get_{nameof(IGrainContext.GrainId)}" ? grainId : null
            );

            FieldInfo field =
                typeof(Grain).GetField(
                    "<GrainContext>k__BackingField",
                    BindingFlags.Instance | BindingFlags.NonPublic
                ) ?? throw new InvalidOperationException("Grain.GrainContext backing field moved.");

            field.SetValue(grain, context);
        }
    }

    private sealed class StubCatalogService(CatalogOfferSnapshot offer) : ICatalogService
    {
        public CatalogSnapshot GetCatalogSnapshot(CatalogType catalogType) =>
            CatalogSnapshot.Empty with
            {
                OffersById = ImmutableDictionary<int, CatalogOfferSnapshot>.Empty.Add(
                    offer.Id,
                    offer
                ),
            };
    }

    private sealed class RecordingEventPublisher(List<IEvent> sink) : IEventPublisher
    {
        public Task PublishAsync(IEvent @event, CancellationToken ct = default)
        {
            sink.Add(@event);

            return Task.CompletedTask;
        }
    }
}
