using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Preferences;

/// <summary>
/// Removes one word from the player's personal filter, by the word rather than by any row id — the
/// client's list only ever holds the strings.
/// </summary>
public record RemoveFromCustomFilterMessage : IMessageEvent
{
    public string Word { get; init; } = string.Empty;
}
