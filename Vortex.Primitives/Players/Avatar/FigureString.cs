using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;

namespace Vortex.Primitives.Players.Avatar;

/// <summary>
/// Reading the figure set ids out of an avatar's look.
/// </summary>
/// <remarks>
/// A figure is dot-separated parts, each part dash-separated: a type, the figure set id, then its
/// colours — <c>hd-180-1.ch-3216-66-1.lg-270-82</c>. Only the set id matters here; it is the number
/// <c>figuredata</c> keys a wearable on, and therefore the number ownership is decided by.
/// <para>
/// Deliberately forgiving: a malformed part is skipped rather than rejected. This reads a look in
/// order to check what is being worn, and a part it cannot parse names no set, so it can grant
/// nothing. Refusing the whole figure over one odd segment would block looks for a mistake that
/// costs nothing.
/// </para>
/// </remarks>
public static class FigureString
{
    /// <summary>The distinct figure set ids a look is wearing.</summary>
    public static ImmutableArray<int> SetIdsOf(string? figure)
    {
        if (string.IsNullOrWhiteSpace(figure))
        {
            return [];
        }

        HashSet<int> setIds = [];

        foreach (string part in figure.Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            string[] fields = part.Split('-');

            if (
                fields.Length >= 2
                && int.TryParse(
                    fields[1],
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out int setId
                )
            )
            {
                setIds.Add(setId);
            }
        }

        return [.. setIds];
    }
}
