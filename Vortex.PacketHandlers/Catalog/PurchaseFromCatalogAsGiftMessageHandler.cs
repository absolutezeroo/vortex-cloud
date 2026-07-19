using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Catalog;
using Vortex.Primitives.Catalog.Enums;
using Vortex.Primitives.Catalog.Snapshots;
using Vortex.Primitives.Messages.Incoming.Catalog;
using Vortex.Primitives.Messages.Outgoing.Catalog;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players;
using Vortex.Primitives.Players.Enums.Wallet;
using Vortex.Primitives.Players.Wallet;

namespace Vortex.PacketHandlers.Catalog;

public class PurchaseFromCatalogAsGiftMessageHandler(
    IGrainFactory grainFactory,
    ICatalogService catalogService
) : IMessageHandler<PurchaseFromCatalogAsGiftMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;
    private readonly ICatalogService _catalogService = catalogService;

    public async ValueTask HandleAsync(
        PurchaseFromCatalogAsGiftMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || string.IsNullOrWhiteSpace(message.RecieverName))
        {
            return;
        }

        PlayerId? receiverId = await _grainFactory
            .GetPlayerDirectoryGrain()
            .GetPlayerIdAsync(message.RecieverName, ct)
            .ConfigureAwait(false);

        if (receiverId is null)
        {
            await ctx.SendComposerAsync(new GiftReceiverNotFoundEventMessageComposer(), ct)
                .ConfigureAwait(false);
            return;
        }

        CatalogSnapshot snapshot = _catalogService.GetCatalogSnapshot(CatalogType.Normal);

        if (!snapshot.OffersById.TryGetValue(message.OfferCode, out CatalogOfferSnapshot? offer))
        {
            await ctx.SendComposerAsync(
                    new PurchaseNotAllowedMessageComposer
                    {
                        ErrorType = CatalogPurchaseErrorType.OfferNotFound,
                    },
                    ct
                )
                .ConfigureAwait(false);
            return;
        }

        if (!offer.CanGift)
        {
            await ctx.SendComposerAsync(
                    new PurchaseNotAllowedMessageComposer
                    {
                        ErrorType = CatalogPurchaseErrorType.PurchaseFailed,
                    },
                    ct
                )
                .ConfigureAwait(false);
            return;
        }

        List<WalletDebitRequest> debits = BuildDebitRequests(offer);

        if (debits.Count > 0)
        {
            WalletDebitResult result = await _grainFactory
                .GetPlayerWalletGrain(ctx.PlayerId)
                .TryDebitAsync(debits, ct)
                .ConfigureAwait(false);

            if (!result.Succeeded)
            {
                WalletDebitFailure? failure = result.Failure;
                await ctx.SendComposerAsync(
                        new NotEnoughBalanceMessageComposer
                        {
                            NotEnoughCredits =
                                failure?.CurrencyKind.CurrencyType == CurrencyType.Credits,
                            NotEnoughActivityPoints =
                                failure?.CurrencyKind.CurrencyType == CurrencyType.ActivityPoints,
                            ActivityPointType = failure?.CurrencyKind.ActivityPointType ?? 0,
                        },
                        ct
                    )
                    .ConfigureAwait(false);
                return;
            }
        }

        await _grainFactory
            .GetInventoryGrain(receiverId.Value)
            .GrantCatalogOfferAsync(offer, message.ExtraParam ?? string.Empty, 1, ct)
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(new PurchaseOKMessageComposer { Offer = offer }, ct)
            .ConfigureAwait(false);
    }

    private static List<WalletDebitRequest> BuildDebitRequests(CatalogOfferSnapshot offer)
    {
        List<WalletDebitRequest> list = new List<WalletDebitRequest>();

        if (offer.CostCredits > 0)
        {
            list.Add(
                new WalletDebitRequest
                {
                    CurrencyKind = new CurrencyKind { CurrencyType = CurrencyType.Credits },
                    Amount = offer.CostCredits,
                }
            );
        }

        if (offer.CostSilver > 0)
        {
            list.Add(
                new WalletDebitRequest
                {
                    CurrencyKind = new CurrencyKind { CurrencyType = CurrencyType.Silver },
                    Amount = offer.CostSilver,
                }
            );
        }

        if (offer.CostCurrency > 0)
        {
            list.Add(
                new WalletDebitRequest
                {
                    CurrencyKind = new CurrencyKind
                    {
                        CurrencyType = CurrencyType.ActivityPoints,
                        ActivityPointType = offer.CurrencyTypeId,
                    },
                    Amount = offer.CostCurrency,
                }
            );
        }

        return list;
    }
}
