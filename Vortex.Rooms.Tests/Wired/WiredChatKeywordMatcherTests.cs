using FluentAssertions;
using Vortex.Rooms.Wired;
using Xunit;

namespace Vortex.Rooms.Tests.Wired;

/// <summary>
/// Locks the match semantics of the wired "avatar says (keyword)" trigger against the client's
/// three-way selector (AvatarSaysSomething.ts): contains / exact / all, case-insensitive, trimmed,
/// with an empty keyword never matching in the keyword modes.
/// </summary>
public sealed class WiredChatKeywordMatcherTests
{
    [Theory]
    [InlineData("hello world", "world", true)]
    [InlineData("hello world", "WORLD", true)] // case-insensitive
    [InlineData("HELLO WORLD", "world", true)]
    [InlineData("hello there", "world", false)]
    [InlineData("swordfish", "word", true)] // substring, not word-boundary
    public void Contains_MatchesSubstring_CaseInsensitive(
        string message,
        string keyword,
        bool expected
    )
    {
        WiredChatKeywordMatcher
            .Matches(message, keyword, WiredChatKeywordMatcher.MatchContains)
            .Should()
            .Be(expected);
    }

    [Theory]
    [InlineData("open", "open", true)]
    [InlineData("  open  ", "open", true)] // trimmed on both sides
    [InlineData("OPEN", "open", true)] // case-insensitive
    [InlineData("open sesame", "open", false)] // extra text fails exact
    [InlineData("ope", "open", false)]
    public void Exact_MatchesWholeMessage_TrimmedCaseInsensitive(
        string message,
        string keyword,
        bool expected
    )
    {
        WiredChatKeywordMatcher
            .Matches(message, keyword, WiredChatKeywordMatcher.MatchExact)
            .Should()
            .Be(expected);
    }

    [Theory]
    [InlineData("anything at all", "")]
    [InlineData("", "ignored")]
    [InlineData("literally whatever", "ignored")]
    public void All_MatchesEveryMessage_IgnoringKeyword(string message, string keyword)
    {
        WiredChatKeywordMatcher
            .Matches(message, keyword, WiredChatKeywordMatcher.MatchAll)
            .Should()
            .BeTrue();
    }

    [Theory]
    [InlineData(WiredChatKeywordMatcher.MatchContains)]
    [InlineData(WiredChatKeywordMatcher.MatchExact)]
    public void EmptyKeyword_NeverMatches_InKeywordModes(int matchType)
    {
        WiredChatKeywordMatcher.Matches("some message", "", matchType).Should().BeFalse();
        WiredChatKeywordMatcher.Matches("some message", null, matchType).Should().BeFalse();
    }
}
