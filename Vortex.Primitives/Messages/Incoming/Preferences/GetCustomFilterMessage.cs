using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Preferences;

/// <summary>
/// Asks for the player's personal word filter. Sent once, when the word-filter dialog is built.
/// Carries no payload.
/// </summary>
public record GetCustomFilterMessage : IMessageEvent;
