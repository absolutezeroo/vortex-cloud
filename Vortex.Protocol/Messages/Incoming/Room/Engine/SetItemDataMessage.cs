using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Room.Engine;

/// <summary>
/// Writes a sticky note: its paper colour and its text.
/// </summary>
/// <remarks>
/// The two travel as separate fields here and are stored as one string, because that is how the
/// client reads them back — it splits the item's data on the first space and treats everything after
/// it as the body, newlines included.
/// </remarks>
public record SetItemDataMessage : IMessageEvent
{
    public required int ItemId { get; init; }

    /// <summary>Six hex digits, no leading hash. The client matches it against its own eight papers
    /// and falls back to yellow on anything else.</summary>
    public required string ColorHex { get; init; }

    public required string Text { get; init; }
}
