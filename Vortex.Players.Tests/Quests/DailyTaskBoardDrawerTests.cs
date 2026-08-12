using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Vortex.Players.Quests;
using Xunit;

namespace Vortex.Players.Tests.Quests;

/// <summary>
///     Which tasks a player gets today. The property that matters is determinism: a reconnect must
///     redraw the same board, or a player could reroll until they liked what they got.
/// </summary>
public sealed class DailyTaskBoardDrawerTests
{
    private static readonly int[] Pool = [10, 11, 12, 13, 14, 15];

    private static readonly DateOnly Day = new(2026, 8, 12);

    [Fact]
    public void Draw_IsStableForTheSamePlayerAndDay()
    {
        IReadOnlyList<int> first = DailyTaskBoardDrawer.Draw(Pool, playerId: 7, Day, count: 3);
        IReadOnlyList<int> second = DailyTaskBoardDrawer.Draw(Pool, playerId: 7, Day, count: 3);

        second.Should().Equal(first);
    }

    [Fact]
    public void Draw_ChangesWithTheDay()
    {
        IReadOnlyList<int> today = DailyTaskBoardDrawer.Draw(Pool, playerId: 7, Day, count: 3);
        IReadOnlyList<int> tomorrow = DailyTaskBoardDrawer.Draw(
            Pool,
            playerId: 7,
            Day.AddDays(1),
            count: 3
        );

        tomorrow.Should().NotEqual(today);
    }

    [Fact]
    public void Draw_GivesDifferentPlayersDifferentBoards()
    {
        // Not a hard guarantee for every pair, but a whole hotel sharing one board would be obvious
        // and wrong; two adjacent ids landing on the same offset is the case to catch.
        IReadOnlyList<int> mine = DailyTaskBoardDrawer.Draw(Pool, playerId: 7, Day, count: 3);
        IReadOnlyList<int> theirs = DailyTaskBoardDrawer.Draw(Pool, playerId: 8, Day, count: 3);

        theirs.Should().NotEqual(mine);
    }

    [Fact]
    public void Draw_ReturnsTheWholePool_WhenItIsSmallerThanTheRequestedCount()
    {
        // A hotel with three tasks configured should hand out three, not fail to draw.
        DailyTaskBoardDrawer.Draw([1, 2], playerId: 7, Day, count: 5).Should().Equal(1, 2);
    }

    [Fact]
    public void Draw_ReturnsNothing_ForAnEmptyPool()
    {
        DailyTaskBoardDrawer.Draw([], playerId: 7, Day, count: 3).Should().BeEmpty();
    }

    [Fact]
    public void Draw_ReturnsNothing_WhenNoneAreAsked()
    {
        DailyTaskBoardDrawer.Draw(Pool, playerId: 7, Day, count: 0).Should().BeEmpty();
    }

    [Fact]
    public void Draw_NeverRepeatsATaskWithinOneBoard()
    {
        IReadOnlyList<int> board = DailyTaskBoardDrawer.Draw(Pool, playerId: 3, Day, count: 4);

        board.Should().HaveCount(4);
        board.Distinct().Should().HaveCount(4);
    }

    [Fact]
    public void Draw_StaysInsideThePool_ForANegativePlayerId()
    {
        // The modulus has to stay non-negative; a stray negative id would otherwise index out of the
        // pool and throw on the first draw.
        DailyTaskBoardDrawer
            .Draw(Pool, playerId: -99, Day, count: 2)
            .Should()
            .OnlyContain(id => Pool.Contains(id));
    }
}
