using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Preferences;

public record SetIgnoreRoomInvitesMessage : IMessageEvent
{
    // OtherSettingsView ignore_room_invites_checkbox sends the ignored flag — header 1332.
    public required bool Ignored { get; init; }
}
