using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Collectibles;

/// <summary>
/// Claiming. Two strings, both of which the client defaults to empty — it sends the pair whether or
/// not it filled them in, so an empty one is a real value and not an absent field.
/// </summary>
public record ClaimNftClaimsMessage : IMessageEvent
{
    public required string ClaimId { get; init; }

    public required string Wallet { get; init; }
}
