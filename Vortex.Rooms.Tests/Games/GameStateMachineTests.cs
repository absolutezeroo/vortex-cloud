using FluentAssertions;
using Vortex.Primitives.Rooms.Games;
using Vortex.Rooms.Games.Runtime;
using Xunit;

namespace Vortex.Rooms.Tests.Games;

/// <summary>
/// The lifecycle table. It exists because the shape it replaced was four booleans in four files with
/// nothing anywhere saying which combinations were legal, so these tests are the specification: an
/// out-of-order start or a double end must be impossible rather than merely unlikely.
/// </summary>
public sealed class GameStateMachineTests
{
    [Theory]
    [InlineData(GamePhase.Idle, GamePhase.Preparing)]
    [InlineData(GamePhase.Preparing, GamePhase.Countdown)]
    [InlineData(GamePhase.Preparing, GamePhase.Running)]
    [InlineData(GamePhase.Countdown, GamePhase.Running)]
    [InlineData(GamePhase.Running, GamePhase.RoundEnding)]
    [InlineData(GamePhase.RoundEnding, GamePhase.Finished)]
    [InlineData(GamePhase.RoundEnding, GamePhase.Preparing)]
    [InlineData(GamePhase.Finished, GamePhase.Resetting)]
    [InlineData(GamePhase.Resetting, GamePhase.Idle)]
    public void TheLegalTransitions_AreAllowed(GamePhase from, GamePhase to) =>
        GameStateMachine.CanTransition(from, to).Should().BeTrue();

    [Theory]
    [InlineData(GamePhase.Idle, GamePhase.Running)]
    [InlineData(GamePhase.Idle, GamePhase.RoundEnding)]
    [InlineData(GamePhase.Running, GamePhase.Preparing)]
    [InlineData(GamePhase.Running, GamePhase.Finished)]
    [InlineData(GamePhase.Countdown, GamePhase.RoundEnding)]
    [InlineData(GamePhase.Finished, GamePhase.Running)]
    [InlineData(GamePhase.Resetting, GamePhase.Running)]
    public void TheIllegalTransitions_AreRejected(GamePhase from, GamePhase to) =>
        GameStateMachine.CanTransition(from, to).Should().BeFalse();

    [Theory]
    [InlineData(GamePhase.Preparing)]
    [InlineData(GamePhase.Countdown)]
    [InlineData(GamePhase.Running)]
    [InlineData(GamePhase.RoundEnding)]
    [InlineData(GamePhase.Finished)]
    public void EveryPhaseWithAMatch_CanFallToCleanup(GamePhase from) =>
        GameStateMachine
            .CanTransition(from, GamePhase.Resetting)
            .Should().BeTrue("cleanup is never skipped, whatever went wrong");

    [Fact]
    public void IdleDoesNotResetAndNoPhaseTransitionsToItself()
    {
        GameStateMachine
            .CanTransition(GamePhase.Idle, GamePhase.Resetting)
            .Should().BeFalse("there is nothing to clean up");

        foreach (GamePhase phase in System.Enum.GetValues<GamePhase>())
        {
            GameStateMachine.CanTransition(phase, phase).Should().BeFalse();
        }
    }

    [Fact]
    public void OnlyRunningIsLive_AndOnlyIdleHasNoMatch()
    {
        foreach (GamePhase phase in System.Enum.GetValues<GamePhase>())
        {
            GameStateMachine.IsLive(phase).Should().Be(phase == GamePhase.Running);
            GameStateMachine.HasMatch(phase).Should().Be(phase != GamePhase.Idle);
        }
    }
}
