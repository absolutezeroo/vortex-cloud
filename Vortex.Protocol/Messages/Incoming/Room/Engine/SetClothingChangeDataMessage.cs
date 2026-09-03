using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Room.Engine;

/// <summary>
/// Sets one of the two outfits a clothing-change booth offers.
/// </summary>
/// <remarks>
/// One message carries one gender's look, and the booth holds both: the client reads the item's data
/// as <c>"&lt;boy&gt;,&lt;girl&gt;"</c> and takes the half matching the avatar in front of it. So the
/// server merges rather than overwrites — setting the girl's look must not erase the boy's.
/// </remarks>
public record SetClothingChangeDataMessage : IMessageEvent
{
    public required int ItemId { get; init; }

    /// <summary>The client sends <c>M</c> or <c>F</c>.</summary>
    public required string Gender { get; init; }

    public required string Look { get; init; }
}
