using System.Collections.Immutable;
using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Players.Grains;

namespace Vortex.Protocol.Messages.Outgoing.Nft;

/// <summary>
/// The whole avatars a player may wear, listed in the avatar editor's own tab.
/// </summary>
/// <remarks>
/// The client asks for this the moment the editor is built, every time, and simply shows nothing if
/// no answer comes -- there is no loading state to get stuck in, unlike the inventory's collectibles
/// tab. An empty list is therefore a legitimate answer and the ordinary one.
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record UserNftWardrobeMessageComposer : IComposer
{
    [Id(0)]
    public required ImmutableArray<NftAvatarSnapshot> Avatars { get; init; }
}
