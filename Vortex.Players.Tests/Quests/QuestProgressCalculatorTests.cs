using FluentAssertions;
using Vortex.Players.Quests;
using Xunit;

namespace Vortex.Players.Tests.Quests;

public class QuestProgressCalculatorTests
{
    [Theory]
    [InlineData(0, 1, 1, 1, true)] // one-step quest completes in one go
    [InlineData(0, 1, 5, 1, false)] // first of five steps
    [InlineData(4, 1, 5, 5, true)] // final step completes
    [InlineData(3, 10, 5, 5, true)] // overshoot is capped and completes
    [InlineData(2, 1, 5, 3, false)] // mid-progress
    public void Apply_AdvancesAndCapsSteps(
        int current,
        int amount,
        int total,
        int expectedSteps,
        bool expectedCompleted
    )
    {
        (int newSteps, bool completed) = QuestProgressCalculator.Apply(current, amount, total);

        newSteps.Should().Be(expectedSteps);
        completed.Should().Be(expectedCompleted);
    }
}
