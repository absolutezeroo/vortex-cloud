using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Userdefinedroomevents.Wiredtrading;

/// <summary>
/// The chest's notification dialog, saved.
/// </summary>
/// <remarks>
/// <see cref="NotificationMode"/> is the dialog's "when": 0 always, 1 only while the owner is not in
/// the room. The five flags are its five checkboxes, named after their own labels.
/// </remarks>
public record SaveWiredChestNotificationSettingsMessage : IMessageEvent
{
    public required int ChestId { get; init; }

    public required int NotificationMode { get; init; }

    public required bool NotifyWhenFull { get; init; }

    public required bool NotifyOnDonation { get; init; }

    public required bool NotifyOnWithdraw { get; init; }

    public required bool NotifyWhenEmpty { get; init; }

    public required bool NotifyOnAnyWiredTransaction { get; init; }
}
