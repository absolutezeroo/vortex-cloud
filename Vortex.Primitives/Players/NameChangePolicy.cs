using System;
using Vortex.Primitives.Players.Enums;

namespace Vortex.Primitives.Players;

/// <summary>
/// Shape rules for a player name — everything that can be decided without touching storage.
/// Uniqueness is not decided here; that is the player directory's job.
/// </summary>
/// <remarks>
/// The client never validates locally: the onboarding dialog asks the server on every pause in
/// typing and renders whatever code comes back. So this is the only place the rules exist, and a
/// name that passes here is one the client will let the user claim.
/// </remarks>
public static class NameChangePolicy
{
    /// <summary>
    /// Letters, digits, and the three separators Habbo names have always allowed.
    /// </summary>
    private static bool IsAllowedCharacter(char value) =>
        char.IsLetterOrDigit(value) || value is '-' or '.' or '_';

    /// <summary>
    /// Validates the shape of <paramref name="name"/>.
    /// </summary>
    /// <returns>
    /// <see cref="NameChangeResultCode.Ok"/> when the name is well formed, otherwise the code the
    /// client turns into a message.
    /// </returns>
    public static NameChangeResultCode Validate(string? name, int minLength, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return NameChangeResultCode.NameRequired;
        }

        if (name.Length < minLength)
        {
            return NameChangeResultCode.NameTooShort;
        }

        if (name.Length > maxLength)
        {
            return NameChangeResultCode.NameTooLong;
        }

        foreach (char character in name)
        {
            if (!IsAllowedCharacter(character))
            {
                return NameChangeResultCode.NameNotValid;
            }
        }

        return NameChangeResultCode.Ok;
    }

    /// <summary>
    /// Builds the alternatives the client shows when a name is taken.
    /// </summary>
    /// <remarks>
    /// Suffixes are appended within <paramref name="maxLength"/>, so a suggestion is always itself
    /// a claimable name — a suggestion the server would then reject is worse than none.
    /// </remarks>
    public static string[] BuildSuggestions(string name, int maxLength, int count)
    {
        if (string.IsNullOrWhiteSpace(name) || count <= 0)
        {
            return [];
        }

        string[] suggestions = new string[count];

        for (int index = 0; index < count; index++)
        {
            string suffix = (index + 1).ToString();
            int keep = Math.Min(name.Length, Math.Max(1, maxLength - suffix.Length));

            suggestions[index] = string.Concat(name.AsSpan(0, keep), suffix);
        }

        return suggestions;
    }
}
