using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Collectibles;

/// <summary>
/// Asks for the player's collectible assets — the inventory's Collectibles tab opening, or a trade
/// starting.
/// </summary>
/// <remarks>
/// Named after what it does rather than after its header constant, which used to call it
/// <c>NftTransferAssets</c>. It transfers nothing; the transfer is
/// <see cref="TransferNftAssetsMessage" />, a different message on a different header. The two names
/// were a permutation of each other and the handler behind this one had inherited the other's
/// documentation.
/// </remarks>
public record GetNftAssetInventoryMessage : IMessageEvent { }
