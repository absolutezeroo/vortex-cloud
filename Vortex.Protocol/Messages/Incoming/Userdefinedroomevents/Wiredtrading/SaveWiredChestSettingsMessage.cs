using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Userdefinedroomevents.Wiredtrading;

/// <summary>
/// The chest's settings dialog, saved.
/// </summary>
/// <remarks>
/// Field names come from the dialog's own labels rather than from guesswork: "Everyone can open the
/// chest", "Everyone can donate to the chest", "Chest state", "Items to preview in open state",
/// "Amount of preview items".
/// </remarks>
public record SaveWiredChestSettingsMessage : IMessageEvent
{
    public required int ChestId { get; init; }

    public required string Name { get; init; }

    public required string Description { get; init; }

    public required bool EveryoneCanOpen { get; init; }

    public required bool EveryoneCanDonate { get; init; }

    public required int ChestState { get; init; }

    public required int PreviewItems { get; init; }

    public required int PreviewAmount { get; init; }

    /// <summary>The dialog telling us whether its own upgrade button was greyed out. Read so the
    /// rest of the message lines up, and then dropped: it is the client describing its own UI.</summary>
    public required bool UpgradeButtonDisabled { get; init; }
}
