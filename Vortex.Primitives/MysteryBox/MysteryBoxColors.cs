using System;
using System.Collections.Immutable;

namespace Vortex.Primitives.MysteryBox;

/// <summary>
/// The mystery box/key colours the client can draw. This list is client truth, not a tunable: the
/// toolbar tracker and the wait dialog tint their artwork through
/// <c>MysteryBoxToolbarExtension.KEY_COLORS</c>, a hardcoded dictionary keyed by exactly these eight
/// lowercase names, and their tooltips resolve <c>${mysterybox.tracker.box.&lt;colour&gt;}</c>. A
/// colour outside the list would render untinted with a raw localization key, so colours are
/// validated here before they can reach the wire.
/// </summary>
public static class MysteryBoxColors
{
    public static readonly ImmutableArray<string> All =
    [
        "purple",
        "blue",
        "green",
        "yellow",
        "lilac",
        "orange",
        "turquoise",
        "red",
    ];

    /// <summary>Lowercases and trims a stored colour, returning empty when it is not renderable.</summary>
    public static string Normalize(string? color)
    {
        if (string.IsNullOrWhiteSpace(color))
        {
            return string.Empty;
        }

        string normalized = color.Trim().ToLowerInvariant();

        return All.Contains(normalized, StringComparer.Ordinal) ? normalized : string.Empty;
    }

    public static bool IsValid(string? color) => Normalize(color).Length > 0;
}
