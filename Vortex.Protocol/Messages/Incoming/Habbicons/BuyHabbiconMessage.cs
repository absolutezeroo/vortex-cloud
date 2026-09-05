using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Habbicons;

/// <summary>Buy one Habbicon at the price on its definition.</summary>
public sealed record BuyHabbiconMessage : IMessageEvent
{
    public required int HabbiconId { get; init; }
}
