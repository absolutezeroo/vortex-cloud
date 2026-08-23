using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Preferences;

public record SetRoomCameraPreferencesMessage : IMessageEvent
{
    // OtherSettingsView disable_room_camera_follow_checkbox sends the disabled flag — header 3917.
    public required bool Disabled { get; init; }
}
