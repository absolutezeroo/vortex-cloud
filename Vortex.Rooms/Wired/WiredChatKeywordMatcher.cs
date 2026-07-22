using System;

namespace Vortex.Rooms.Wired;

/// <summary>
/// Pure keyword-match logic for the wired "avatar says (keyword)" trigger, mirroring the client's
/// match-type selector (AvatarSaysSomething.ts): 0 = the message contains the text, 1 = the message
/// is exactly the text, 2 = any message. All comparisons are case-insensitive and trim surrounding
/// whitespace.
/// </summary>
public static class WiredChatKeywordMatcher
{
    public const int MatchContains = 0;
    public const int MatchExact = 1;
    public const int MatchAll = 2;

    public static bool Matches(string? message, string? keyword, int matchType)
    {
        if (matchType == MatchAll)
        {
            return true;
        }

        if (string.IsNullOrEmpty(keyword))
        {
            return false;
        }

        string text = message ?? string.Empty;

        return matchType == MatchExact
            ? text.Trim().Equals(keyword.Trim(), StringComparison.OrdinalIgnoreCase)
            : text.Contains(keyword, StringComparison.OrdinalIgnoreCase);
    }
}
