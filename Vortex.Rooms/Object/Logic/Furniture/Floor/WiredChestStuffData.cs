using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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

    /// <summary>
    /// The item kinds the chest floats above itself while open, as
    /// <c>isWallItem,spriteId[,extra]</c> joined by <c>;</c>.
    /// </summary>
    /// <remarks>
    /// Read by the client's furni-chest logic, which splits on <c>;</c>, then on <c>,</c>, and asks
    /// the room engine for one icon per entry — see
    /// <c>habbo/room/object/logic/furniture/_SafeCls_1812.as::stringToItemType()</c> in the WIN63
    /// client. The boolean is compared against the literal <c>"true"</c>, so it is spelled out
    /// rather than sent as 1/0, and the third field is omitted entirely when empty because the
    /// client tests <c>parts.length &gt; 2</c>.
    /// </remarks>
    public const string Visuals = "visuals";

    /// <summary>
    /// The furni's own state, which for map stuff data lives in the map under this key.
    /// </summary>
    /// <remarks>
    /// Two things read it, and both matter here: the visualization picks the chest's appearance
    /// from it, and the furni-chest logic blanks <see cref="Visuals" /> unless it is odd — the
    /// preview only shows on an open chest. Odd is open.
    /// </remarks>
    public const string State = "state";

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

    /// <summary>Writes the chest's open/closed appearance.</summary>
    /// <remarks>
    /// Odd is open. Only the parity matters to the preview, so the two states are 1 and 0 rather
    /// than a pair picked out of the furni's own animation list — which the server does not read.
    /// </remarks>
    public static void ApplyState(IMapStuffData map, bool open) =>
        map.Data[State] = open ? "1" : "0";

    /// <summary>Writes the preview list an open chest floats above itself.</summary>
    /// <remarks>
    /// An empty list writes an empty string rather than dropping the key: the client only clears
    /// the icons it is already showing when the value it reads back differs from the last one, so a
    /// missing key would leave the last preview on screen forever.
    /// </remarks>
    public static void ApplyPreview(IMapStuffData map, IEnumerable<ChestPreviewKind> kinds) =>
        map.Data[Visuals] = string.Join(';', kinds.Select(Encode));

    private static string Encode(ChestPreviewKind kind)
    {
        string head = $"{(kind.IsWallItem ? "true" : "false")},{Number(kind.SpriteId)}";

        return string.IsNullOrEmpty(kind.Extra) ? head : $"{head},{kind.Extra}";
    }
}

/// <summary>One entry of a chest's preview: what to draw an icon of, not which item it was.</summary>
/// <remarks>
/// The client asks for icons by kind — sprite, wall-or-floor, and the poster number when the kind is
/// a poster — exactly as its withdraw request names one. Two identical items are one kind and would
/// draw the same icon twice, which is what the "prefer different item types" preview modes exist to
/// avoid.
/// </remarks>
internal readonly record struct ChestPreviewKind(bool IsWallItem, int SpriteId, string Extra);
