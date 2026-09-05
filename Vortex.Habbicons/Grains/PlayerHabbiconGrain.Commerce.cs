using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vortex.Database.Context;
using Vortex.Database.Entities.Habbicons;
using Vortex.Primitives.Catalog.Snapshots;
using Vortex.Primitives.Commerce;
using Vortex.Primitives.Events;
using Vortex.Primitives.Habbicons;
using Vortex.Primitives.Habbicons.Snapshots;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players.Enums.Wallet;
using Vortex.Primitives.Players.Grains;
using Vortex.Primitives.Players.Wallet;
using Vortex.Protocol.Messages.Outgoing.Catalog;

namespace Vortex.Habbicons.Grains;

/// <summary>
/// Buying Habbicons, and claiming the bonus a completed collection unlocks.
/// </summary>
/// <remarks>
/// Every path here goes through <see cref="WalletPurchaseExtensions.ExecutePurchaseAsync"/> rather
/// than a hand-written debit: it is the one place that puts the money back when the grant throws,
/// and a Habbicon purchase is exactly as capable of failing after the debit as a catalogue one.
/// </remarks>
internal sealed partial class PlayerHabbiconGrain
{
    public async Task BuyHabbiconAsync(int habbiconId, CancellationToken ct)
    {
        if (
            !catalog.TryGetHabbicon(habbiconId, out HabbiconDefinitionSnapshot? definition)
            || definition.IsCollectionReward
        )
        {
            // A bonus Habbicon is claimed, never bought. Refusing here rather than pricing it means
            // there is no path that sells the thing completing the set is supposed to earn.
            await SendPurchaseErrorAsync().ConfigureAwait(true);

            return;
        }

        if (!definition.IsAvailableAt(DateTime.UtcNow) || !definition.HasPrice)
        {
            await SendPurchaseErrorAsync().ConfigureAwait(true);

            return;
        }

        if (_owned.ContainsKey(habbiconId))
        {
            await SendPurchaseErrorAsync().ConfigureAwait(true);

            return;
        }

        await PurchaseAsync(
                [definition],
                definition.PriceCredits,
                definition.PriceActivityPoints,
                definition.ActivityPointType,
                HabbiconSource.Shop,
                $"habbicon={habbiconId}",
                definition.Code,
                ct
            )
            .ConfigureAwait(true);
    }

    public async Task BuyCollectionAsync(int collectionId, CancellationToken ct)
    {
        if (
            !catalog.TryGetCollection(collectionId, out HabbiconCollectionSnapshot? collection)
            || !collection.IsAvailableAt(DateTime.UtcNow)
            || !collection.HasPrice
        )
        {
            await SendPurchaseErrorAsync().ConfigureAwait(true);

            return;
        }

        List<HabbiconDefinitionSnapshot> missing = HabbiconCollectionRules.MissingEntries(
            collection,
            StateMap()
        );

        if (missing.Count == 0)
        {
            // Nothing to sell. Charging the set price for zero Habbicons is the failure mode worth
            // being explicit about.
            await SendPurchaseErrorAsync().ConfigureAwait(true);

            return;
        }

        await PurchaseAsync(
                missing,
                collection.PriceCredits,
                collection.PriceActivityPoints,
                collection.ActivityPointType,
                HabbiconSource.ShopCollection,
                $"habbicon-collection={collectionId} missing={missing.Count}",
                collection.Code,
                ct
            )
            .ConfigureAwait(true);
    }

    public async Task ClaimCollectionRewardAsync(int habbiconId, CancellationToken ct)
    {
        if (
            !catalog.TryGetHabbicon(habbiconId, out HabbiconDefinitionSnapshot? definition)
            || !definition.IsCollectionReward
            || !catalog.TryGetCollection(
                definition.CollectionId,
                out HabbiconCollectionSnapshot? collection
            )
        )
        {
            return;
        }

        // Recomputed from stored ownership, never from what the client thinks it is looking at. The
        // client marks a bonus claimable on its own the moment its album fills up, so this is the
        // only place the claim is actually earned.
        if (!HabbiconCollectionRules.CanClaimReward(collection, StateMap()))
        {
            return;
        }

        HabbiconGrantResult granted = await GrantAsync(
                habbiconId,
                HabbiconSource.CollectionReward,
                ct
            )
            .ConfigureAwait(true);

        // Idempotent by construction: the grant refuses to write a second row, so a repeated claim
        // -- a double click, a reconnect replaying the packet -- reports the state it already had.
        if (!granted.Succeeded || !granted.WasNew)
        {
            return;
        }

        await SendShopAsync().ConfigureAwait(true);

        await events
            .PublishAsync(
                new HabbiconCollectionRewardClaimedEvent(
                    PlayerId,
                    collection.CollectionId,
                    collection.Code,
                    habbiconId
                ),
                ct
            )
            .ConfigureAwait(true);

        logger.LogInformation(
            "Player {PlayerId} claimed the bonus Habbicon {HabbiconId} of collection {CollectionCode}.",
            PlayerId,
            habbiconId,
            collection.Code
        );
    }

    /// <summary>
    /// Debits once and writes every bought row in one commit. Shared by the single and whole-set
    /// purchases because they differ only in how many Habbicons they hand over.
    /// </summary>
    private async Task PurchaseAsync(
        IReadOnlyList<HabbiconDefinitionSnapshot> buying,
        int priceCredits,
        int priceActivityPoints,
        int activityPointType,
        HabbiconSource source,
        string description,
        string localizationCode,
        CancellationToken ct
    )
    {
        List<WalletDebitRequest> debits = [];

        if (priceCredits > 0)
        {
            debits.Add(
                new WalletDebitRequest
                {
                    CurrencyKind = new CurrencyKind { CurrencyType = CurrencyType.Credits },
                    Amount = priceCredits,
                }
            );
        }

        if (priceActivityPoints > 0)
        {
            debits.Add(
                new WalletDebitRequest
                {
                    CurrencyKind = new CurrencyKind
                    {
                        CurrencyType = CurrencyType.ActivityPoints,
                        ActivityPointType = activityPointType,
                    },
                    Amount = priceActivityPoints,
                }
            );
        }

        CommerceOperationId operation = CommerceOperationId.New();

        await journal
            .OpenAsync(
                operation,
                CommerceOperationKind.HabbiconPurchase,
                PlayerId,
                $"{description} credits={priceCredits} points={priceActivityPoints}",
                ct
            )
            .ConfigureAwait(true);

        try
        {
            WalletPurchaseResult<bool> result = await grainFactory
                .GetPlayerWalletGrain(PlayerId)
                .ExecutePurchaseAsync(
                    debits,
                    operation,
                    async innerCt =>
                    {
                        await journal
                            .TransitionAsync(
                                operation,
                                CommerceOperationState.Debited,
                                CommerceStepKeys.DEBIT,
                                null,
                                innerCt
                            )
                            .ConfigureAwait(true);

                        await WriteOwnershipAsync(buying, source, innerCt).ConfigureAwait(true);

                        await journal
                            .TransitionAsync(
                                operation,
                                CommerceOperationState.Completed,
                                CommerceStepKeys.HABBICON_GRANT,
                                null,
                                innerCt
                            )
                            .ConfigureAwait(true);

                        return true;
                    },
                    logger,
                    ct,
                    journal
                )
                .ConfigureAwait(true);

            if (!result.Succeeded)
            {
                await journal
                    .TransitionAsync(
                        operation,
                        CommerceOperationState.FailedBeforePivot,
                        CommerceStepKeys.DEBIT,
                        "insufficient balance",
                        ct
                    )
                    .ConfigureAwait(true);

                await SendPurchaseErrorAsync().ConfigureAwait(true);

                return;
            }
        }
        catch (Exception ex)
        {
            // The executor has already refunded and recorded it. The client still has a
            // confirmation dialog spinning, so it gets the same answer an ordinary refusal gets.
            logger.LogError(
                ex,
                "Habbicon purchase failed after the debit for player {PlayerId} ({Description}); the balance was refunded.",
                PlayerId,
                description
            );

            await SendPurchaseErrorAsync().ConfigureAwait(true);

            return;
        }

        foreach (HabbiconDefinitionSnapshot bought in buying)
        {
            await events
                .PublishAsync(
                    new HabbiconGrantedEvent(
                        PlayerId,
                        bought.HabbiconId,
                        bought.CollectionId,
                        source
                    ),
                    ct
                )
                .ConfigureAwait(true);
        }

        // Completion is checked once, against the state after every row landed -- a whole-set
        // purchase completes the set with its last row, and checking per row would raise the event
        // in the middle of a batch that is not finished writing.
        await RaiseCollectionCompletionIfNewAsync(buying[^1], ct).ConfigureAwait(true);

        await SendPurchaseOkAsync(
                localizationCode,
                priceCredits,
                priceActivityPoints,
                activityPointType
            )
            .ConfigureAwait(true);

        await SendShopAsync().ConfigureAwait(true);
        await PushInventoryAsync(ct).ConfigureAwait(true);

        logger.LogInformation(
            "Player {PlayerId} bought {Count} Habbicon(s) ({Description}) for {Credits} credits and {Points} activity points.",
            PlayerId,
            buying.Count,
            description,
            priceCredits,
            priceActivityPoints
        );
    }

    /// <summary>
    /// Writes every bought ownership row in one commit, skipping anything already owned. One
    /// <c>SaveChanges</c> rather than a loop of them, so a whole-set purchase either lands or does
    /// not.
    /// </summary>
    private async Task WriteOwnershipAsync(
        IReadOnlyList<HabbiconDefinitionSnapshot> buying,
        HabbiconSource source,
        CancellationToken ct
    )
    {
        DateTime now = DateTime.UtcNow;

        await using VortexDbContext db = await dbCtxFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(true);

        foreach (HabbiconDefinitionSnapshot definition in buying)
        {
            if (_owned.ContainsKey(definition.HabbiconId))
            {
                continue;
            }

            db.PlayerHabbicons.Add(
                new PlayerHabbiconEntity
                {
                    PlayerEntityId = PlayerId,
                    HabbiconEntityId = definition.HabbiconId,
                    State = HabbiconState.Owned,
                    Source = source,
                    AcquiredAt = now,
                }
            );
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(true);

        foreach (HabbiconDefinitionSnapshot definition in buying)
        {
            _owned.TryAdd(
                definition.HabbiconId,
                new OwnedHabbicon(HabbiconState.Owned, now, source, null)
            );
        }
    }

    /// <summary>
    /// The catalogue's own purchase acknowledgement, which is what the client listens for: the
    /// Habbicon controller subscribes to PurchaseOK/PurchaseError alongside the catalogue rather
    /// than having results of its own, and its confirmation dialog closes on nothing else.
    /// </summary>
    /// <remarks>
    /// The offer is synthetic and carries no products, which serializes to an empty product list.
    /// The client reads the block and ignores every field of it — <c>onPurchaseOk</c> only closes
    /// the dialog and re-asks for the shop — but it does read it, so the block has to be there.
    /// </remarks>
    private Task SendPurchaseOkAsync(
        string localizationCode,
        int priceCredits,
        int priceActivityPoints,
        int activityPointType
    ) =>
        Presence.SendComposerAsync(
            new PurchaseOKMessageComposer
            {
                Offer = new CatalogOfferSnapshot
                {
                    Id = 0,
                    PageId = 0,
                    LocalizationId = localizationCode,
                    Rentable = false,
                    CostCredits = priceCredits,
                    CostCurrency = priceActivityPoints,
                    CurrencyTypeId = activityPointType,
                    CostSilver = 0,
                    CanGift = false,
                    CanBundle = false,
                    ClubLevel = 0,
                    Visible = true,
                    ProductIds = [],
                    Products = [],
                },
            }
        );

    /// <summary>
    /// A refusal. Sent for every failed path so the confirmation dialog stops spinning and says so;
    /// a silent refusal reads to the player as the button being broken.
    /// </summary>
    private Task SendPurchaseErrorAsync() =>
        Presence.SendComposerAsync(new PurchaseErrorMessageComposer { ErrorCode = 0 });
}
