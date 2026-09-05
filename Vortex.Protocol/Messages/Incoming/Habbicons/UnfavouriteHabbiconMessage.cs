using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Habbicons;

/// <summary>Un-star a starred Habbicon.</summary>
public sealed record UnfavouriteHabbiconMessage : IMessageEvent
{
    public required int HabbiconId { get; init; }
}
