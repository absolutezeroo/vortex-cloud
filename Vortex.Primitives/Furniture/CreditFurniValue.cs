using System;
using System.Globalization;

namespace Vortex.Primitives.Furniture;

/// <summary>
/// What a credit furni is worth, read out of its classname.
/// </summary>
/// <remarks>
/// The value lives nowhere else: all 135 credit definitions in this catalogue carry a null
/// <c>extra_data</c>, and furnidata has no field for it. The convention is Habbo's own —
/// <c>CF_50_coin_gold</c>, <c>CF_1000_goldenkey</c> — and the client relies on it too, which is why
/// parsing the name is the reading rather than a shortcut.
/// </remarks>
public static class CreditFurniValue
{
    private const string Prefix = "CF_";

    /// <summary>
    /// Returns false for any name that does not carry a positive value in the expected position, so
    /// a mislabelled definition redeems for nothing instead of for zero credits or a crash.
    /// </summary>
    public static bool TryParse(string className, out int credits)
    {
        credits = 0;

        if (
            string.IsNullOrEmpty(className)
            || !className.StartsWith(Prefix, StringComparison.Ordinal)
        )
        {
            return false;
        }

        int valueStart = Prefix.Length;
        int valueEnd = className.IndexOf('_', valueStart);

        if (valueEnd <= valueStart)
        {
            return false;
        }

        return int.TryParse(
                className.AsSpan(valueStart, valueEnd - valueStart),
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out credits
            )
            && credits > 0;
    }
}
