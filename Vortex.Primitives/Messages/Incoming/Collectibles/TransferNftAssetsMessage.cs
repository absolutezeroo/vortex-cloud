using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Collectibles;

/// <summary>
/// Actually moving the assets to a wallet, from the transfer tab's confirm button.
///
/// Distinct from <see cref="NftTransferAssetsMessage"/> despite the near-identical name: that one
/// is header 1646, <c>CollectiblesModel::requestNftAssets()</c>, which only asks for the list. This
/// is 1749 and does the transfer. The older name was already taken by the read, so the write got
/// the qualified one.
/// </summary>
public record TransferNftAssetsMessage : IMessageEvent
{
    public required string Wallet { get; init; }
}
