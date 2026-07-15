using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Incoming.Userdefinedroomevents.Wiredmenu;

public record WiredSetPreferencesMessage : IMessageEvent
{
    public required bool WiredMenuButton { get; init; }
    public required bool WiredInspectButton { get; init; }
    public required bool PlayTestMode { get; init; }
    public required bool WiredWhisperDisabled { get; init; }
    public required bool ShowAllNotifications { get; init; }
    public required string UiStyle { get; init; }
}
