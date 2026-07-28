using System;
using Vortex.Primitives.Navigator.Enums;

namespace Vortex.Primitives.Navigator;

/// <summary>
/// Splits the client's <c>&lt;prefix&gt;:&lt;value&gt;</c> filtering-data string into a typed filter.
/// The client builds these itself (for example <c>group:</c> from the guild quick-search and
/// <c>roomname:</c> from the room-name one), so every search entry point must parse them the same
/// way — a raw pass-through silently degrades a guild search into a room-name search.
/// </summary>
public static class NavigatorSearchFilter
{
    public static (NavigatorSearchFilterType Type, string Value) Parse(string? filteringData)
    {
        if (string.IsNullOrWhiteSpace(filteringData))
        {
            return (NavigatorSearchFilterType.Anything, string.Empty);
        }

        int splitIndex = filteringData.IndexOf(':', StringComparison.Ordinal);

        if (splitIndex <= 0)
        {
            return (NavigatorSearchFilterType.Anything, filteringData);
        }

        return (
            NavigatorSearchFilterTypeExtensions.FromLegacyString(filteringData[..splitIndex]),
            filteringData[(splitIndex + 1)..]
        );
    }

    /// <summary>Re-serializes a parsed filter back into the client's wire form.</summary>
    public static string Format(NavigatorSearchFilterType type, string value) =>
        type == NavigatorSearchFilterType.Anything ? value : $"{type.ToLegacyString()}:{value}";
}
