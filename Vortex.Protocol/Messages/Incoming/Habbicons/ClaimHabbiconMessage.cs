using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Habbicons;

/// <summary>
/// Claim a completed collection's bonus Habbicon. The id sent is the bonus Habbicon's, not the
/// collection's -- the client only ever knows the reward it is looking at.
/// </summary>
public sealed record ClaimHabbiconMessage : IMessageEvent
{
    public required int HabbiconId { get; init; }
}
