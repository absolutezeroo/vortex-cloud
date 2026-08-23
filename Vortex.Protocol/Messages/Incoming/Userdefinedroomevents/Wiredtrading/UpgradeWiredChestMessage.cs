using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Userdefinedroomevents.Wiredtrading;

/// <summary>The "buy" button on a chest's upgrade dialog.</summary>
public record UpgradeWiredChestMessage : IMessageEvent
{
    public required int ChestId { get; init; }

    /// <summary>Which upgrade was bought. The client offers one per row of the dialog.</summary>
    public required int UpgradeType { get; init; }
}
