using FluentAssertions;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Actions;
using Xunit;

namespace Vortex.Rooms.Tests.Bots;

/// <summary>
/// Both bot-talk wireds pack two fields into one string param, the way the client's setup form
/// writes them: the bot's name, a tab, then the line to say.
/// </summary>
public sealed class WiredBotTalkConfigurationTests
{
    [Fact]
    public void TheNameAndTheLine_AreSplitOnTheClientsDelimiter()
    {
        (string botName, string text) = WiredActionBotTalk.SplitConfiguration("Frank\tWelcome in!");

        botName.Should().Be("Frank");
        text.Should().Be("Welcome in!");
    }

    [Fact]
    public void OnlyTheFirstDelimiterSeparates()
    {
        // The message box is a free text area, so a tab inside the line belongs to the line.
        (string botName, string text) = WiredActionBotTalk.SplitConfiguration("Frank\tone\ttwo");

        botName.Should().Be("Frank");
        text.Should().Be("one\ttwo");
    }

    [Fact]
    public void AFormSavedWithNoMessage_YieldsANameAndNothingToSay()
    {
        (string botName, string text) = WiredActionBotTalk.SplitConfiguration("Frank");

        botName.Should().Be("Frank");
        text.Should()
            .BeEmpty("the action does nothing rather than making the bot say its own name");
    }

    [Fact]
    public void AnEmptyForm_YieldsNothing()
    {
        (string botName, string text) = WiredActionBotTalk.SplitConfiguration(null);

        botName.Should().BeEmpty();
        text.Should().BeEmpty();
    }
}
