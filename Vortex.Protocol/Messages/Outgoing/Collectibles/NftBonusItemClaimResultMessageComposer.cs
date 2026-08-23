using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Collectibles;

/// <summary>
/// The outcome of claiming an item into a wallet. Two strings then a boolean -- and the boolean is
/// the whole answer, so an empty body used to read as "false" by luck rather than by intent while
/// also leaving the client two strings short.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record NftBonusItemClaimResultMessageComposer : IComposer
{
    [Id(0)]
    public required string CollectionId { get; init; }

    [Id(1)]
    public required string WalletAddress { get; init; }

    [Id(2)]
    public required bool Success { get; init; }
}
