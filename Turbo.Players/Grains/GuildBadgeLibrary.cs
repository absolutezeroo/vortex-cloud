using System;
using System.Collections.Generic;
using Turbo.Primitives.Groups.Snapshots;

namespace Turbo.Players.Grains;

/// <summary>
///     Helpers for guild badge code encoding / decoding. Badge part catalogue data
///     (base shapes, symbols, colours) is loaded from the database via
///     <c>IGuildBadgePartProvider</c>.
/// </summary>
internal static class GuildBadgeLibrary
{
    // Hard client constant: the badge editor always has exactly 5 layers (BadgeLayerCtrl × 5).
    // The server must always send exactly this many GuildBadgeSettings entries.
    public const int LayerCount = 5;

    private static readonly GroupBadgePartSnapshot EmptyLayer = new()
    {
        PartId = 0,
        ColorId = 0,
        Position = 0,
    };

    /// <summary>
    ///     The badge the creation wizard pre-fills. Layer 0 is base shape 1, colour 1, centred
    ///     (position 4); layers 1–4 are empty. Always exactly <see cref="LayerCount" /> entries.
    /// </summary>
    public static List<GroupBadgePartSnapshot> DefaultBadgeParts { get; } =
    [
        new()
        {
            PartId = 1,
            ColorId = 1,
            Position = 4,
        },
        EmptyLayer,
        EmptyLayer,
        EmptyLayer,
        EmptyLayer,
    ];

    /// <summary>
    ///     Parses a stored badge code (e.g. <c>b010104b020201</c>) into exactly
    ///     <see cref="LayerCount" /> layer entries, padding with empty layers as needed.
    /// </summary>
    /// <remarks>
    ///     Each segment: <c>b{partId:D2}{colorId:D2}{position:D1}</c> (6 chars).
    /// </remarks>
    public static List<GroupBadgePartSnapshot> ParseBadgeCode(string? badgeCode)
    {
        List<GroupBadgePartSnapshot> result = new(LayerCount);

        if (!string.IsNullOrEmpty(badgeCode))
        {
            int i = 0;
            while (i + 5 < badgeCode.Length && result.Count < LayerCount)
            {
                if (badgeCode[i] != 'b')
                {
                    i++;
                    continue;
                }

                if (
                    int.TryParse(badgeCode.AsSpan(i + 1, 2), out int partId)
                    && int.TryParse(badgeCode.AsSpan(i + 3, 2), out int colorId)
                    && int.TryParse(badgeCode.AsSpan(i + 5, 1), out int position)
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

        while (result.Count < LayerCount)
        {
            result.Add(EmptyLayer);
        }

        return result;
    }
}
