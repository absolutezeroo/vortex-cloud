using System.Collections.Generic;
using FluentAssertions;
using Vortex.Progression.Achievements;
using Vortex.Progression.Grains;
using Xunit;

namespace Vortex.Players.Tests.Achievements;

/// <summary>
///     The level the profile shows. This replaced a hardcoded 1, so the case that matters most is
///     simply that the number moves at all — and that it moves on the right side of a threshold.
/// </summary>
public sealed class AccountLevelLadderTests
{
    private static readonly (int Level, int RequiredScore)[] Ladder =
    [
        (1, 0),
        (2, 50),
        (3, 125),
        (4, 250),
    ];

    [Theory]
    [InlineData(0, 1)]
    [InlineData(49, 1)]
    [InlineData(50, 2)] // exactly on the threshold counts as reached
    [InlineData(124, 2)]
    [InlineData(125, 3)]
    [InlineData(10_000, 4)] // past the top rung, stays on the last one
    public void Resolve_MovesWithTheScore(int score, int expected)
    {
        AccountLevelLadder.Resolve(Ladder, score).Should().Be(expected);
    }

    [Fact]
    public void Resolve_IsTheFloor_WhenNoLadderIsConfigured()
    {
        // A hotel that never seeded the ladder shows "Level 1", not "Level 0" — which is what the
        // client would print, since it renders the number verbatim.
        AccountLevelLadder.Resolve([], achievementScore: 9_000).Should().Be(1);
    }

    [Fact]
    public void Resolve_ClampsANegativeScore()
    {
        AccountLevelLadder.Resolve(Ladder, achievementScore: -500).Should().Be(1);
    }

    [Fact]
    public void Resolve_HandlesRungsEnteredOutOfOrder()
    {
        List<(int, int)> scrambled = [(4, 250), (2, 50), (1, 0), (3, 125)];

        AccountLevelLadder.Resolve(scrambled, achievementScore: 200).Should().Be(3);
    }

    [Fact]
    public void Resolve_StopsAtTheFirstUnreachedRung()
    {
        // A gap in the ladder must not let a far-off rung be picked up: reaching 60 is level 2,
        // never level 4, even though rung 4 is listed after it.
        List<(int, int)> gapped = [(1, 0), (2, 50), (4, 5_000)];

        AccountLevelLadder.Resolve(gapped, achievementScore: 60).Should().Be(2);
    }
}
