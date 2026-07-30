using System;
using System.Globalization;

namespace Vortex.Primitives.MysteryBox;

/// <summary>
/// Legacy stuff-data layout of an inscribed trophy. The client splits the string on tabs into
/// owner / date / message (<c>FurnitureTrophyWidgetHandler</c> takes everything up to the first tab
/// as the name, then up to the next as the date, then the rest as the message), so a tab inside any
/// field would shift every later field.
/// </summary>
public static class MysteryTrophyStuffData
{
    private const char Separator = '\t';

    public static string Build(string ownerName, DateOnly date, string inscription) =>
        string.Concat(
            Sanitize(ownerName),
            Separator,
            date.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture),
            Separator,
            Sanitize(inscription)
        );

    /// <summary>Strips the field separator and the newlines the client would render literally.</summary>
    private static string Sanitize(string value) =>
        string.IsNullOrEmpty(value)
            ? string.Empty
            : value.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ').Trim();
}
