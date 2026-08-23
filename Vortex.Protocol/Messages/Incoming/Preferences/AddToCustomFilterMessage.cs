using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Preferences;

/// <summary>
/// Adds one word to the player's personal filter. The client applies nothing locally: it waits for
/// <see cref="Outgoing.Preferences.ModifyCustomFilterResultMessageComposer"/> before showing it.
/// </summary>
public record AddToCustomFilterMessage : IMessageEvent
{
    public string Word { get; init; } = string.Empty;
}
