using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Nft;

/// <summary>
/// Putting on one of the avatars the player owns, chosen in the editor's own tab.
/// </summary>
/// <remarks>
/// Carries the identity the tile was listed under — the copy's id, as a string, which is also the
/// number the player sees printed after the "#" beneath it.
/// </remarks>
public record SaveUserNftWardrobeMessage : IMessageEvent
{
    public required string CopyId { get; init; }
}
