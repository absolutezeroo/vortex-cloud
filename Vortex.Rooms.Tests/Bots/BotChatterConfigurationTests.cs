using FluentAssertions;
using Vortex.Primitives.Bots;
using Xunit;

namespace Vortex.Rooms.Tests.Bots;

/// <summary>
/// The chatter dialog sends one string holding four fields. Reading it as a plain semicolon-
/// separated phrase list — which is what the room did — left a bot reciting its own settings:
/// "true", "10" and "false" became lines like any other.
/// </summary>
public sealed class BotChatterConfigurationTests
{
    [Fact]
    public void TheFieldsAreReadOffTheClientsOwnMarker_NotOffSemicolons()
    {
        BotChatterConfiguration chatter = BotChatterConfiguration.Parse(
            "hello\rgoodbye;#;true;#;25;#;false"
        );

        chatter.Phrases.Should().Equal("hello", "goodbye");
        chatter.AutoChat.Should().BeTrue();
        chatter.DelaySeconds.Should().Be(25);
        chatter.Markov.Should().BeFalse();
    }

    [Fact]
    public void APhraseMayContainASemicolon_WhichIsWhyTheMarkerExists()
    {
        BotChatterConfiguration chatter = BotChatterConfiguration.Parse(
            "wait; then what?;#;true;#;10;#;false"
        );

        chatter.Phrases.Should().Equal("wait; then what?");
    }

    [Fact]
    public void TheLegacyThreeFieldForm_IsStillRead()
    {
        // The client's own parser accepts it, so a bot configured before the marker existed must
        // not start reciting "true" and "10".
        BotChatterConfiguration chatter = BotChatterConfiguration.Parse("hi;1;30");

        chatter.Phrases.Should().Equal("hi");
        chatter.AutoChat.Should().BeTrue("the legacy form writes 1 rather than true");
        chatter.DelaySeconds.Should().Be(30);
    }

    [Fact]
    public void AutoChatOff_IsCarriedThroughRatherThanAssumed()
    {
        BotChatterConfiguration chatter = BotChatterConfiguration.Parse(
            "hello;#;false;#;10;#;false"
        );

        chatter.Phrases.Should().Equal("hello");
        chatter.AutoChat.Should().BeFalse();
    }

    [Fact]
    public void ADelayBelowTheFloor_IsRaisedToIt()
    {
        BotChatterConfiguration chatter = BotChatterConfiguration.Parse("hello;#;true;#;0;#;false");

        chatter
            .DelaySeconds.Should()
            .Be(
                BotChatterConfiguration.MinimumDelaySeconds,
                "a bot on a zero-second timer fills the room's chat on its own"
            );
    }

    [Fact]
    public void ANonsenseDelay_FallsBackRatherThanThrowing()
    {
        BotChatterConfiguration chatter = BotChatterConfiguration.Parse(
            "hello;#;true;#;soon;#;false"
        );

        chatter.DelaySeconds.Should().Be(BotChatterConfiguration.DefaultDelaySeconds);
    }

    [Fact]
    public void BlankLinesAndPadding_DoNotBecomePhrases()
    {
        BotChatterConfiguration chatter = BotChatterConfiguration.Parse(
            "  hello  \r\n\r  bye \r;#;true;#;10;#;false"
        );

        chatter.Phrases.Should().Equal("hello", "bye");
    }

    [Fact]
    public void AnUnconfiguredBot_ParsesToSomethingSilent()
    {
        BotChatterConfiguration chatter = BotChatterConfiguration.Parse(null);

        chatter.Phrases.Should().BeEmpty();
        chatter.AutoChat.Should().BeFalse();
    }
}
