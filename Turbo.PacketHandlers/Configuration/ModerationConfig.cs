using System.Collections.Generic;

namespace Turbo.PacketHandlers.Configuration;

public sealed class ModerationConfig
{
    public const string SECTION_NAME = "Turbo:Moderation";

    /// <summary>
    /// The staff CFH tool's mute action carries no duration on the wire (unlike the in-room mute
    /// panel, which sends explicit minutes) — this is the server-side default applied for it.
    /// </summary>
    public int ModToolDefaultMuteDurationMinutes { get; init; } = 60;

    /// <summary>
    /// Maps the client's ModBan "sanctionTypeId" preset to a ban duration in hours (null = permanent).
    /// The WIN63 client sends whichever preset id the staff member picked from its own local list —
    /// these hour values are a reasonable default matching common Habbo ban-tier conventions, not
    /// confirmed against the client's actual preset labels. Tune via config if they don't match.
    /// </summary>
    public IReadOnlyDictionary<int, int?> BanDurationHoursBySanctionType { get; init; } =
        new Dictionary<int, int?>
        {
            [0] = 2, // 2 hours
            [1] = 24, // 1 day
            [2] = 72, // 3 days
            [3] = 168, // 1 week
            [4] = 720, // 1 month
            [5] = null, // permanent
        };

    /// <summary>Same shape as <see cref="BanDurationHoursBySanctionType"/>, for ModTradingLock's
    /// "lockDurationTypeId". Same caveat: default tiers, not confirmed against client presets.</summary>
    public IReadOnlyDictionary<int, int?> TradingLockDurationHoursByType { get; init; } =
        new Dictionary<int, int?>
        {
            [0] = 24, // 1 day
            [1] = 168, // 1 week
            [2] = 720, // 1 month
            [3] = null, // permanent
        };

    /// <summary>Fallback ban duration (hours) when the client sends a sanctionTypeId not present in
    /// <see cref="BanDurationHoursBySanctionType"/> — short and logged, erring toward low blast radius
    /// over silently under- or over-punishing on a misconfigured/unrecognized preset id.</summary>
    public int UnknownSanctionTypeFallbackHours { get; init; } = 2;
}
