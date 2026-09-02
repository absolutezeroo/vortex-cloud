using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Vortex.Catalog.Exceptions;
using Vortex.Primitives.Catalog.Enums;
using Vortex.Primitives.Catalog.Snapshots;
using Vortex.Primitives.Commerce;
using Xunit;

namespace Vortex.Database.Tests.Catalog;

/// <summary>
/// The two catalogue entry points that are not a plain purchase: buying a room advertisement, which
/// pays for a service rather than an item, and opening a present, which pays for nothing because it
/// was paid for when it was bought. Both had no tests at all.
/// </summary>
public sealed class CatalogRoomAdAndPresentTests
{
    private const int BUYER_ID = 11;
    private const int OFFER_ID = 900;
    private const int ROOM_ID = 77;

    private static Task<CatalogOfferSnapshot> BuyAdAsync(
        CatalogPurchaseHarness harness,
        bool extended = false
    ) =>
        harness.Grain.PurchaseRoomAdAsync(
            OFFER_ID,
            ROOM_ID,
            "my room",
            "come in",
            extended,
            categoryId: 1,
            CancellationToken.None
        );

    private static CatalogPurchaseHarness AdHarness(int days, int costCredits = 25) =>
        new(
            CatalogOffers.New(OFFER_ID, costCredits),
            BUYER_ID,
            CatalogOffers.NewProduct(OFFER_ID, days)
        );

    /// <summary>
    /// A room ad's duration is not a configured field: it rides on the product's Quantity, the same
    /// slot a furni product uses for how many copies you get. Nothing else in the purchase reveals
    /// how long the advertisement was actually bought for.
    /// </summary>
    [Fact]
    public async Task TheAdvertisement_RunsForTheDaysTheProductEncodes()
    {
        CatalogPurchaseHarness harness = AdHarness(days: 3);
        DateTime before = DateTime.UtcNow;

        await BuyAdAsync(harness).ConfigureAwait(true);

        (int roomId, string name, DateTime expiresAt) = harness.AdvertisementsCreated.Single();
        roomId.Should().Be(ROOM_ID);
        name.Should().Be("my room");
        expiresAt.Should().BeCloseTo(before.AddDays(3), TimeSpan.FromMinutes(1));
        harness.DebitRequests.Single().Amount.Should().Be(25);

        // Buying an advertisement is a purchase like any other, and was the third flow found with
        // no operation id: credits left the buyer and nothing anywhere recorded that they had.
        harness
            .Journal.Opened.Should()
            .ContainSingle()
            .Which.Kind.Should()
            .Be(CommerceOperationKind.RoomAdPurchase);
        harness
            .Journal.Transitions.Should()
            .Contain(t => t.State == CommerceOperationState.Completed);
    }

    /// <summary>"Extended" doubles the run rather than naming a second duration per offer.</summary>
    [Fact]
    public async Task Extended_DoublesTheRun()
    {
        CatalogPurchaseHarness harness = AdHarness(days: 3);
        DateTime before = DateTime.UtcNow;

        await BuyAdAsync(harness, extended: true).ConfigureAwait(true);

        harness
            .AdvertisementsCreated.Single()
            .ExpiresAt.Should()
            .BeCloseTo(before.AddDays(6), TimeSpan.FromMinutes(1));
    }

    /// <summary>An offer carrying no product still buys a day rather than an advertisement that has
    /// already expired.</summary>
    [Fact]
    public async Task AnOfferWithNoProduct_StillRunsForADay()
    {
        CatalogPurchaseHarness harness = new(CatalogOffers.New(OFFER_ID, 25), BUYER_ID);
        DateTime before = DateTime.UtcNow;

        await BuyAdAsync(harness).ConfigureAwait(true);

        harness
            .AdvertisementsCreated.Single()
            .ExpiresAt.Should()
            .BeCloseTo(before.AddDays(1), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task InsufficientBalance_CreatesNoAdvertisement()
    {
        CatalogPurchaseHarness harness = AdHarness(days: 3);
        harness.DebitSucceeds = false;

        Func<Task> act = () => BuyAdAsync(harness);

        (await act.Should().ThrowAsync<CatalogPurchaseException>().ConfigureAwait(true))
            .Which.ErrorType.Should()
            .Be(CatalogPurchaseErrorType.NotEnoughCredits);
        harness.AdvertisementsCreated.Should().BeEmpty();
    }

    /// <summary>The advertisement is the whole of what the credits bought, so failing to create it
    /// has to give them back.</summary>
    [Fact]
    public async Task AnAdvertisementThatFails_RefundsTheBuyer()
    {
        CatalogPurchaseHarness harness = AdHarness(days: 3);
        harness.AdvertisementThrows = true;

        Func<Task> act = () => BuyAdAsync(harness);

        await act.Should().ThrowAsync<InvalidOperationException>().ConfigureAwait(true);
        harness.CreditBackCalls.Should().Be(1);
        harness.AdvertisementsCreated.Should().BeEmpty();
    }

    [Fact]
    public async Task AnUnknownOffer_IsRefusedBeforeAnyDebit()
    {
        CatalogPurchaseHarness harness = AdHarness(days: 3);

        Func<Task> act = () =>
            harness.Grain.PurchaseRoomAdAsync(
                OFFER_ID + 1,
                ROOM_ID,
                "my room",
                null,
                false,
                1,
                CancellationToken.None
            );

        (await act.Should().ThrowAsync<CatalogPurchaseException>().ConfigureAwait(true))
            .Which.ErrorType.Should()
            .Be(CatalogPurchaseErrorType.OfferNotFound);
        harness.DebitRequests.Should().BeEmpty();
    }

    /// <summary>
    /// Opening a present is the one catalogue path that grants without charging: the credits were
    /// taken when the present was bought. So the invariant is not about money, it is that the
    /// contents come out exactly once and that nothing is debited on the way.
    /// </summary>
    [Fact]
    public async Task OpeningAPresent_GrantsTheContentsAndChargesNothing()
    {
        CatalogPurchaseHarness harness = new(CatalogOffers.New(OFFER_ID, 50), BUYER_ID);

        CatalogOfferSnapshot? offer = await harness
            .Grain.GrantPresentContentsAsync(OFFER_ID, string.Empty, CancellationToken.None)
            .ConfigureAwait(true);

        offer!.Id.Should().Be(OFFER_ID);
        harness.GrantedToPlayerIds.Should().Equal(BUYER_ID);
        harness.GrantedQuantities.Should().Equal(1);
        harness.DebitRequests.Should().BeEmpty();
        harness.DebitedPlayerIds.Should().BeEmpty();
    }

    /// <summary>
    /// A present outlives the catalogue page it came from, so an offer that has since been removed
    /// is an ordinary outcome. It has to hand back nothing rather than throw: the player is holding
    /// a box, and an exception here would leave them holding it forever.
    /// </summary>
    [Fact]
    public async Task APresentWhoseOfferIsGone_GrantsNothingAndDoesNotThrow()
    {
        CatalogPurchaseHarness harness = new(CatalogOffers.New(OFFER_ID, 50), BUYER_ID);

        CatalogOfferSnapshot? offer = await harness
            .Grain.GrantPresentContentsAsync(OFFER_ID + 1, string.Empty, CancellationToken.None)
            .ConfigureAwait(true);

        offer.Should().BeNull();
        harness.GrantedQuantities.Should().BeEmpty();
    }
}
