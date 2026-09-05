using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Preferences;

/// <summary>
/// The four Discord Rich Presence toggles the settings dialog saves, plus the version of the consent
/// dialog the player answered (header 2304).
/// </summary>
/// <remarks>
/// <see cref="Version"/> is not ours to choose: the client sends back
/// <c>discord_activity.settings.version</c> from <c>external_variables</c>. It is what decides
/// whether the opt-in popup shows again — the client compares the stored version against the current
/// one, so raising the variable re-asks every player, and storing it verbatim is the whole
/// mechanism.
/// </remarks>
public record SetDiscordPreferencesMessage : IMessageEvent
{
    public required int Version { get; init; }

    public required bool ShowHabbo { get; init; }

    public required bool ShareActivity { get; init; }

    public required bool HideInHiddenRooms { get; init; }

    public required bool AllowJoining { get; init; }
}
