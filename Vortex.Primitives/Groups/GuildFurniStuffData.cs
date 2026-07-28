using System.Collections.Generic;

namespace Vortex.Primitives.Groups;

/// <summary>
/// Layout of the string-array stuff data carried by guild-customized furni (guild gates, badges,
/// banners, forum terminals). AS3-verified against <c>FurnitureGuildCustomizedLogic</c>, which reads
/// the array by these exact indices and exposes them to the visualization as
/// <c>furniture_guild_customized_guild_id</c>, the badge asset to load, and the two recolour slots.
/// </summary>
public static class GuildFurniStuffData
{
    /// <summary>Multi-state value (gate open/closed, etc.) — shared with every other furni.</summary>
    public const int StateIndex = 0;

    public const int GuildIdIndex = 1;
    public const int BadgeCodeIndex = 2;

    /// <summary>Primary recolour, as a bare RGB hex string with no leading '#'.</summary>
    public const int ColorOneIndex = 3;

    /// <summary>Secondary recolour, as a bare RGB hex string with no leading '#'.</summary>
    public const int ColorTwoIndex = 4;

    /// <summary>Client defaults, applied when a colour id cannot be resolved to a palette entry.</summary>
    public const string DefaultColorOne = "eeeeee";
    public const string DefaultColorTwo = "4b4b4b";

    private const string ClosedState = "0";

    /// <summary>
    /// Builds the five-element array the client expects. The client indexes all five unconditionally,
    /// so a short array leaves the badge and both recolours unset.
    /// </summary>
    public static List<string> Build(
        int guildId,
        string badgeCode,
        string colorOneHex,
        string colorTwoHex,
        string state = ClosedState
    ) =>
        [
            state,
            guildId.ToString(),
            badgeCode,
            string.IsNullOrWhiteSpace(colorOneHex) ? DefaultColorOne : colorOneHex,
            string.IsNullOrWhiteSpace(colorTwoHex) ? DefaultColorTwo : colorTwoHex,
        ];
}
