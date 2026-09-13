using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Room.RaidProtection;

/// <summary>
/// The owner opened the raid-protection panel. Sent from the room-info button, which reaches the
/// controller through the <c>navigator/raidprotection/&lt;roomId&gt;</c> link
/// (<c>RoomInfoViewCtrl.as:498</c> in AIR 1.0.31).
/// </summary>
public record GetRaidProtectionSettingsMessage : IMessageEvent
{
    public required int RoomId { get; init; }
}
