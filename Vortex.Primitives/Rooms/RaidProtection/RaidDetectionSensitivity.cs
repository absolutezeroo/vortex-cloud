namespace Vortex.Primitives.Rooms.RaidProtection;

/// <summary>
/// How eagerly a room decides that the people walking in are a raid. The three values are the
/// client's own — <c>RaidProtectionSettingsData</c> in AIR 1.0.31 accepts 0, 1 and 2 and refuses to
/// send anything else — but what each one means in entries per minute is ours: the official server
/// never tells the client a threshold, so no client build can be read for one.
/// </summary>
public enum RaidDetectionSensitivity
{
    Low = 0,
    Medium = 1,
    High = 2,
}
