using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Preferences;

/// <summary>
/// The player's saved Discord Rich Presence preferences (header 2767), the only answer to both
/// <c>GetDiscordPreferences</c> and a save.
/// </summary>
/// <remarks>
/// <see cref="Version"/> 0 means "never answered". The client treats it specially: it substitutes
/// its own all-on defaults for display and, if the local Discord client is connected, shows the
/// opt-in popup five seconds later. Sending a real version for a player who never saved would
/// silently suppress that popup forever.
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record DiscordPreferencesEventMessageComposer : IComposer
{
    [Id(0)]
    public required int Version { get; init; }

    [Id(1)]
    public required bool ShowHabbo { get; init; }

    [Id(2)]
    public required bool ShareActivity { get; init; }

    [Id(3)]
    public required bool HideInHiddenRooms { get; init; }

    [Id(4)]
    public required bool AllowJoining { get; init; }
}
