using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Habbicons;

/// <summary>Star an owned Habbicon.</summary>
public sealed record FavouriteHabbiconMessage : IMessageEvent
{
    public required int HabbiconId { get; init; }
}
