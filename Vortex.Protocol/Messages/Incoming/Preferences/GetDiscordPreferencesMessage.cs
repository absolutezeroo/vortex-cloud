using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Preferences;

/// <summary>
/// Empty request the Discord component sends once at init (header 2883). Unanswered, the client
/// never gets preferences and its own settings dialog refuses to open.
/// </summary>
public record GetDiscordPreferencesMessage : IMessageEvent;
