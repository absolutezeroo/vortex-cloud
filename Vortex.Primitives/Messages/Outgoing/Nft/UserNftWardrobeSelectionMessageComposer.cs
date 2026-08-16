using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Nft;

/// <summary>
/// Which whole avatar the player is wearing, and the look to give them back.
/// </summary>
/// <remarks>
/// <b>Only send this when an avatar is actually worn.</b> The client decides "wearing one" by
/// testing the token id against null, and a string read off a packet is never null -- so an answer
/// meaning "none" convinces it the opposite. It then loads the fallback figure instead of the
/// player's own whenever the editor opens, and with no fallback to load that path does nothing at
/// all: the avatar editor stops showing the player their own look. Silence is the correct way to
/// say nothing is worn.
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record UserNftWardrobeSelectionMessageComposer : IComposer
{
    [Id(0)]
    public required string TokenId { get; init; }

    [Id(1)]
    public required string FallbackFigure { get; init; }

    [Id(2)]
    public required string FallbackGender { get; init; }
}
