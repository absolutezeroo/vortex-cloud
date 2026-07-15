using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Incoming.Userdefinedroomevents.Wiredmenu;

public record WiredSetRoomSettingsMessage : IMessageEvent
{
    public required int ModifyPermissionMask { get; init; }
    public required int ReadPermissionMask { get; init; }
    public required string Timezone { get; init; }
}
