using System.Collections.Generic;

namespace Turbo.PacketHandlers.Configuration;

/// <summary>
/// Per-category point thresholds for the staff/player badge tool's "points needed for next level"
/// display (GetBadgePointLimitsMessageHandler). The client builds a badge's code as
/// "ACH_" + category + level, independent of whatever system eventually tracks a player's actual
/// progress toward these thresholds (no such system exists yet) -- this is a display catalog only.
/// </summary>
public sealed class BadgeConfig
{
    public const string SECTION_NAME = "Turbo:Badges";

    /// <summary>Ordered level limits per category (index 0 = level 1's limit, etc.). Placeholder
    /// defaults loosely matching common Habbo achievement categories -- tune via config once a real
    /// progress-tracking system exists to compare against.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<int>> LimitsByCategory { get; init; } =
        new Dictionary<string, IReadOnlyList<int>>
        {
            ["RoomEntry"] = [1, 5, 15, 30, 50],
            ["Login"] = [1, 5, 15, 30, 60],
            ["Trading"] = [1, 3, 10, 25, 50],
            ["Friends"] = [1, 5, 10, 20, 35],
            ["Post"] = [1, 5, 15, 30, 50],
        };
}
