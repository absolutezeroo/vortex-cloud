using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Habbicons;

/// <summary>Buy every entry of a collection the player is still missing, at the set price.</summary>
public sealed record BuyHabbiconCollectionMessage : IMessageEvent
{
    public required int CollectionId { get; init; }
}
