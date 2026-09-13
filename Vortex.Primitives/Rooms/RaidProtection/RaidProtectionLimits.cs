using System.Collections.Immutable;

namespace Vortex.Primitives.Rooms.RaidProtection;

/// <summary>
/// The exact value sets the official panel offers, read out of <c>RaidProtectionSettingsController</c>
/// in AIR 1.0.31, where they gate the send: the client refuses to compose a save whose fields are
/// not in these lists.
/// </summary>
/// <remarks>
/// Which is why the server checks them too. A client that refuses to send a bad value is a client
/// we do not control — a modified one asking for a 30-year ban reaches the same handler.
/// </remarks>
public static class RaidProtectionLimits
{
    /// <summary>Ban lengths the action dropdown offers: 5 minutes to 7 days.</summary>
    public static readonly ImmutableArray<int> BanDurationsSeconds =
    [
        300,
        900,
        1800,
        3600,
        10800,
        21600,
        43200,
        86400,
        259200,
        604800,
    ];

    /// <summary>Guard lengths: 5 minutes to 3 hours. A shorter list than the bans, deliberately.</summary>
    public static readonly ImmutableArray<int> GuardDurationsSeconds =
    [
        300,
        900,
        1800,
        3600,
        10800,
    ];

    /// <summary>The defaults a room that has never been configured reports.</summary>
    public const int DefaultBanDurationSeconds = 3600;

    /// <inheritdoc cref="DefaultBanDurationSeconds" />
    public const int DefaultGuardDurationSeconds = 900;

    public static bool IsSensitivity(int value) =>
        value is >= (int)RaidDetectionSensitivity.Low and <= (int)RaidDetectionSensitivity.High;

    public static bool IsAction(int value) =>
        value is >= (int)RaidProtectionAction.Kick and <= (int)RaidProtectionAction.TemporaryBan;

    public static bool IsBanDuration(int seconds) => BanDurationsSeconds.Contains(seconds);

    public static bool IsGuardDuration(int seconds) => GuardDurationsSeconds.Contains(seconds);
}
