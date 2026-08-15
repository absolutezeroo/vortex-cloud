using System;
using System.Collections.Generic;

namespace Vortex.Rooms.Wired;

/// <summary>
/// The token a wired text placeholder answers to, and how the box's string param encodes it.
/// </summary>
/// <remarks>
/// The client tells the player, in as many words: "Use this by typing <c>$(name)</c> in Wired
/// texts". The name is lowercased with spaces turned into underscores by the form itself, so the
/// token is built the same way here rather than trusted as typed.
/// <para>
/// The string param is the name alone, or the name and a delimiter separated by a tab when the box
/// is set to show multiple values.
/// </para>
/// </remarks>
public static class WiredPlaceholder
{
    /// <summary>The prefix every placeholder box passes to its name section.</summary>
    private const string Prefix = "$";

    /// <summary>Splits the box's string param into the placeholder name and the delimiter that
    /// joins multiple values. The delimiter is absent unless the box shows multiple.</summary>
    public static (string Name, string Delimiter) ParseConfiguration(string? stringParam)
    {
        string[] parts = (stringParam ?? string.Empty).Split('\t');

        return (parts[0].Trim(), parts.Length > 1 ? parts[1] : string.Empty);
    }

    /// <summary>The literal a player types in a text to reach this placeholder, or empty when the
    /// box was never named — an empty token would otherwise match everywhere.</summary>
    public static string BuildToken(string name)
    {
        string normalized = name.Replace(' ', '_').ToLowerInvariant();

        return normalized.Length == 0 ? string.Empty : $"{Prefix}({normalized})";
    }

    /// <summary>
    /// Replaces every occurrence of the token. Values are joined with the delimiter when the box
    /// shows multiple; otherwise only the first is used, and an empty set removes the token rather
    /// than leaving it on screen.
    /// </summary>
    public static string Substitute(
        string text,
        string token,
        IReadOnlyList<string> values,
        bool showMultiple,
        string delimiter
    )
    {
        if (string.IsNullOrEmpty(token) || !text.Contains(token, StringComparison.Ordinal))
        {
            return text;
        }

        string replacement =
            showMultiple ? string.Join(delimiter, values)
            : values.Count > 0 ? values[0]
            : string.Empty;

        return text.Replace(token, replacement, StringComparison.Ordinal);
    }
}
