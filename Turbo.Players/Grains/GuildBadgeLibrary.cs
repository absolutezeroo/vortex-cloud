using System;
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
    /// The badge a fresh guild starts with in the creation wizard.
    /// <para>
    /// IMPORTANT: The client badge editor always has exactly 5 layers (indices 0–4) and iterates
    /// <c>_badgeInitData[0..4]</c> unconditionally — a missing entry causes a null-reference crash.
    /// We therefore always send exactly 5 <c>GuildBadgeSettings</c> entries: layer 0 (the base
    /// shape) is pre-filled with part 1, colour 1, centred (position 4); layers 1–4 are empty
    /// (partId=0, colorId=0, position=0).
    /// </para>
    /// </summary>
    public static List<GroupBadgePartSnapshot> DefaultBadgeParts { get; } =
        [
            new GroupBadgePartSnapshot { PartId = 1, ColorId = 1, Position = 4 }, // base layer
            new GroupBadgePartSnapshot { PartId = 0, ColorId = 0, Position = 0 }, // overlay 1 (empty)
            new GroupBadgePartSnapshot { PartId = 0, ColorId = 0, Position = 0 }, // overlay 2 (empty)
            new GroupBadgePartSnapshot { PartId = 0, ColorId = 0, Position = 0 }, // overlay 3 (empty)
            new GroupBadgePartSnapshot { PartId = 0, ColorId = 0, Position = 0 }, // overlay 4 (empty)
        ];

    // Number of badge layers the client badge editor always has. This is a hard client constant
    // (5 BadgeLayerCtrl instances created unconditionally); the server must always send exactly
    // this many GuildBadgeSettings entries or the client crashes with a null-reference.
    public const int LayerCount = 5;

    private static readonly GroupBadgePartSnapshot EmptyLayer = new()
    {
        PartId = 0,
        ColorId = 0,
        Position = 0,
    };

    /// <summary>
    /// Parses a stored badge code (e.g. <c>b010104b020201</c>) into exactly <see cref="LayerCount"/>
    /// layer entries, padding with empty layers as needed. The client badge editor iterates all 5
    /// layers unconditionally; a short list causes a null-reference crash.
    /// </summary>
    /// <remarks>
    /// Badge code format per segment: <c>b{partId:D2}{colorId:D2}{position}</c>.
    /// </remarks>
    public static List<GroupBadgePartSnapshot> ParseBadgeCode(string? badgeCode)
    {
        var result = new List<GroupBadgePartSnapshot>(LayerCount);

        if (!string.IsNullOrEmpty(badgeCode))
        {
            // Each segment is 'b' + 2-digit partId + 2-digit colorId + 1-digit position = 6 chars.
            var i = 0;
            while (i + 5 < badgeCode.Length && result.Count < LayerCount)
            {
                if (badgeCode[i] != 'b')
                {
                    i++;
                    continue;
                }

                if (
                    int.TryParse(badgeCode.AsSpan(i + 1, 2), out var partId)
                    && int.TryParse(badgeCode.AsSpan(i + 3, 2), out var colorId)
                    && int.TryParse(badgeCode.AsSpan(i + 5, 1), out var position)
                )
                {
                    result.Add(
                        new GroupBadgePartSnapshot
                        {
                            PartId = partId,
                            ColorId = colorId,
                            Position = position,
                        }
                    );
                }

                i += 6;
            }
        }

        // Pad to exactly LayerCount entries.
        while (result.Count < LayerCount)
            result.Add(EmptyLayer);

        return result;
    }

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
