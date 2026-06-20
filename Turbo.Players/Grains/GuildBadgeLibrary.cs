using System.Collections.Generic;
using System.Linq;
using Turbo.Primitives.Groups.Snapshots;

namespace Turbo.Players.Grains;

/// <summary>
/// Static catalogue of the guild badge designer's building blocks: the selectable base shapes,
/// overlay symbols, and the badge / guild colour swatches the client renders in the badge editor
/// (<c>GuildEditorDataMessageEvent</c>) and the creation wizard's default badge.
///
/// <para>
/// Wire contract (reverse-engineered from the client's <c>BadgePartData</c> /
/// <c>GuildColorData</c>): a part is (id, fileName, maskFileName) and the client loads the asset as
/// <c>badgepart_{fileName}.png</c>; a colour is (id, hex) where the client parses the hex string
/// base-16. The badge code persisted on the group embeds the <em>part ids</em> and <em>colour
/// ids</em> defined here, so these ids must stay stable to keep existing badges rendering.
/// </para>
///
/// <para>
/// File names follow the conventional Habbo scheme (<c>base_N</c> / <c>symbol_N</c>); the actual
/// glyphs come from the client's asset pack. Extend the ranges / palette here to expose more parts
/// — nothing else needs to change.
/// </para>
/// </summary>
internal static class GuildBadgeLibrary
{
    // Number of selectable base shapes and overlay symbols exposed to the editor. Bump these to
    // surface more parts once the matching badgepart_* assets ship in the client pack.
    private const int BaseShapeCount = 16;
    private const int SymbolCount = 60;

    // Standard guild badge colour palette (RRGGBB, no leading '#'); the client parses base-16.
    // These are the classic Habbo guild swatches; ids are 1-based and stable.
    private static readonly string[] PaletteHex =
    [
        "ffffff", // 1  white
        "e7e7e7", // 2  light grey
        "999999", // 3  grey
        "000000", // 4  black
        "ff0000", // 5  red
        "ff7f00", // 6  orange
        "ffd700", // 7  gold
        "ffff00", // 8  yellow
        "00ff00", // 9  green
        "008000", // 10 dark green
        "00ffff", // 11 cyan
        "0000ff", // 12 blue
        "000080", // 13 navy
        "8b00ff", // 14 violet
        "ff00ff", // 15 magenta
        "ff69b4", // 16 pink
        "8b4513", // 17 brown
    ];

    /// <summary>Base shapes that form the bottom layer of a guild badge.</summary>
    public static List<GroupBadgePartOptionSnapshot> BaseParts { get; } = BuildParts(
        "base",
        BaseShapeCount
    );

    /// <summary>Overlay symbols layered on top of the base shape.</summary>
    public static List<GroupBadgePartOptionSnapshot> LayerParts { get; } = BuildParts(
        "symbol",
        SymbolCount
    );

    /// <summary>Colour swatches applied to individual badge parts.</summary>
    public static List<GroupColorOptionSnapshot> BadgeColors { get; } = BuildColors();

    /// <summary>Primary guild colour swatches (figure/room accents); shares the badge palette.</summary>
    public static List<GroupColorOptionSnapshot> PrimaryColors { get; } = BuildColors();

    /// <summary>Secondary guild colour swatches; shares the badge palette.</summary>
    public static List<GroupColorOptionSnapshot> SecondaryColors { get; } = BuildColors();

    /// <summary>
    /// The badge a fresh guild starts with in the creation wizard: a single base shape (part 1)
    /// in the first colour, centred. The player edits from here before confirming.
    /// </summary>
    public static List<GroupBadgePartSnapshot> DefaultBadgeParts { get; } =
        [new GroupBadgePartSnapshot { PartId = 1, ColorId = 1, Position = 4 }];

    /// <summary>
    /// Resolves a colour id (as stored in <c>GroupEntity.ColorOne</c>/<c>ColorTwo</c>) to its hex
    /// string for display. Unknown / unset ids fall back to the first palette entry.
    /// </summary>
    public static string ResolveColorHex(string? colorId)
    {
        if (int.TryParse(colorId, out var id) && id >= 1 && id <= PaletteHex.Length)
            return PaletteHex[id - 1];

        return PaletteHex[0];
    }

    private static List<GroupBadgePartOptionSnapshot> BuildParts(string prefix, int count) =>
        Enumerable
            .Range(1, count)
            .Select(i => new GroupBadgePartOptionSnapshot
            {
                Id = i,
                FileName = $"{prefix}_{i}",
                // No separate mask layer in the default pack; an empty name means "no mask".
                MaskFileName = string.Empty,
            })
            .ToList();

    private static List<GroupColorOptionSnapshot> BuildColors() =>
        PaletteHex
            .Select((hex, index) => new GroupColorOptionSnapshot { Id = index + 1, ColorHex = hex })
            .ToList();
}
