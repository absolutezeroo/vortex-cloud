using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Habbicons;

/// <summary>
/// A request for one Habbicon's shop row (<c>HabbiconController.getHabbiconInfo</c>), answered with
/// a single-item message rather than the whole shop.
/// </summary>
public sealed record GetHabbiconInfoMessage : IMessageEvent
{
    public required int HabbiconId { get; init; }
}
