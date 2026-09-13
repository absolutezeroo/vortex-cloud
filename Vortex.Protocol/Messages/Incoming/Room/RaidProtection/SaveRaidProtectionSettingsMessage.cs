using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Room.RaidProtection;

/// <summary>
/// A draft from the raid-protection panel.
/// </summary>
/// <remarks>
/// Nine fields, not the snapshot's ten plus one: the client composes
/// <c>roomId, enabled, detectionSensitivity, actionType, banDurationSeconds, guardEnabled,
/// guardDurationSeconds, guardSensitivity, confirmed</c> and never sends back the two facts the
/// room owns about itself (<c>incidentActive</c>, <c>lastRaidAt</c>).
/// </remarks>
public record SaveRaidProtectionSettingsMessage : IMessageEvent
{
    public required int RoomId { get; init; }
    public required bool Enabled { get; init; }
    public required int DetectionSensitivity { get; init; }
    public required int ActionType { get; init; }
    public required int BanDurationSeconds { get; init; }
    public required bool GuardEnabled { get; init; }
    public required int GuardDurationSeconds { get; init; }
    public required int GuardSensitivity { get; init; }

    /// <summary>
    /// Set when the player answered the client's confirmation prompt, which it only raises on an
    /// off-to-on transition. False is the normal case for every other save.
    /// </summary>
    public required bool Confirmed { get; init; }
}
