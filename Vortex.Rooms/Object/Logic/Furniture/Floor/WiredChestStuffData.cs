using System.Globalization;
using Vortex.Database.Entities.Wired;
using Vortex.Primitives.Furniture.StuffData;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor;

/// <summary>
/// The stuff-data contract between a wired chest and the client's dialogs.
/// </summary>
/// <remarks>
/// Every chest dialog prefills from the furni's own map stuff data rather than from a message of its
/// own — <c>getValue("chest_name")</c>, <c>getValue("everyone_can_open")</c>,
/// <c>getValue("notify_mode")</c>. These eighteen keys are that contract, read straight off
/// <c>ChestSettingsUI</c>, <c>ChestNotificationSettingsUI</c> and the wrapper view, and they live in
/// one place for the same reason a serializer does: it is a wire format, not domain state, and the
/// day one of them is renamed there should be a single file to change.
/// </remarks>
internal static class WiredChestStuffData
{
    public const string Name = "chest_name";
    public const string Description = "chest_desc";
    public const string EveryoneCanOpen = "everyone_can_open";
    public const string EveryoneCanDonate = "everyone_can_donate";
    public const string StateControlMode = "state_control_mode";
    public const string PreviewMode = "preview_mode";
    public const string PreviewAmount = "preview_amount";
    public const string NotifyMode = "notify_mode";
    public const string NotificationChestFull = "notification_chest_full";
    public const string NotificationDonation = "notification_donation";
    public const string NotificationSomeoneWithdraws = "notification_someone_withdraws";
    public const string NotificationChestEmpty = "notification_chest_empty";
    public const string NotificationWiredTransaction = "notification_wired_transaction";
    public const string Locked = "locked";
    public const string AutoLock = "auto_lock";
    public const string Capacity = "capacity";

    /// <summary>Bought capacity tier. No column behind it yet — it arrives with the upgrade
    /// purchase — but the dialog reads it unconditionally, so it exists as zero.</summary>
    public const string CapacityLevel = "capacity_level";

    /// <summary>Whether the chest answers to wiring. Server-owned: the dialog shows it and never
    /// sends it back.</summary>
    public const string IsWiredEnabled = "is_wired_enabled";

    /// <summary>The client compares these against the string "1", so a bool is not a bool here.</summary>
    private static string Flag(bool value) => value ? "1" : "0";

    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

    /// <summary>Writes a chest's saved settings onto the furni the client is looking at.</summary>
    public static void Apply(IMapStuffData map, WiredChestEntity chest)
    {
        map.Data[Name] = chest.Name;
        map.Data[Description] = chest.Description;
        map.Data[EveryoneCanOpen] = Flag(chest.EveryoneCanOpen);
        map.Data[EveryoneCanDonate] = Flag(chest.EveryoneCanDonate);
        map.Data[StateControlMode] = Number(chest.ChestState);
        map.Data[PreviewMode] = Number(chest.PreviewItems);
        map.Data[PreviewAmount] = Number(chest.PreviewAmount);
        map.Data[NotifyMode] = Number(chest.NotificationMode);
        map.Data[NotificationChestFull] = Flag(chest.NotifyWhenFull);
        map.Data[NotificationDonation] = Flag(chest.NotifyOnDonation);
        map.Data[NotificationSomeoneWithdraws] = Flag(chest.NotifyOnWithdraw);
        map.Data[NotificationChestEmpty] = Flag(chest.NotifyWhenEmpty);
        map.Data[NotificationWiredTransaction] = Flag(chest.NotifyOnAnyWiredTransaction);
        map.Data[Locked] = Flag(chest.Locked);
        map.Data[AutoLock] = Flag(chest.AutoLock);
        map.Data[Capacity] = Number(chest.Capacity);
        map.Data[CapacityLevel] = "0";
        map.Data[IsWiredEnabled] = "0";
    }
}
