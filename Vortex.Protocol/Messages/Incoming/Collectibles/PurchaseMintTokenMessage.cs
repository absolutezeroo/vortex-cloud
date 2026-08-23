using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Collectibles;

/// <summary>
/// Buying a bundle of stamps — the tokens a Relic is minted with — for silver.
/// </summary>
/// <remarks>
/// This does not arrive through the catalogue's ordinary purchase message even though it is sent
/// from the catalogue's purchase dialog: the dialog recognises a stamp offer by its own product
/// type and calls <c>purchaseMintTokens(offerId, wallet)</c> instead, which is this message.
/// </remarks>
public record PurchaseMintTokenMessage : IMessageEvent
{
    /// <summary>Identifies the bundle by row id, unlike the shop, which sends a product code.</summary>
    public required int OfferId { get; init; }

    public required string Wallet { get; init; }
}
