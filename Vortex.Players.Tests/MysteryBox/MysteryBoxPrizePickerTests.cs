using System;
using System.Collections.Generic;
using FluentAssertions;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.MysteryBox;
using Vortex.Primitives.MysteryBox.Snapshots;
using Xunit;

namespace Vortex.Players.Tests.MysteryBox;

/// <summary>
/// Locks the draw the server does on the player's behalf. The client never sends an outcome, so
/// these rules are the only thing standing between a prize pool and someone winning a prize that was
/// meant for another colour, or a disabled/zero-weight row leaking back into circulation.
/// </summary>
public sealed class MysteryBoxPrizePickerTests
{
    private static MysteryBoxPrizeSnapshot Prize(
        int id,
        MysteryBoxPrizePool pool = MysteryBoxPrizePool.Box,
        string color = "",
        int weight = 1
    ) =>
        new()
        {
            Id = id,
            Pool = pool,
            Color = color,
            ProductType = ProductType.Floor,
            FurnitureDefinitionId = id,
            ExtraParam = string.Empty,
            Weight = weight,
        };

    [Fact]
    public void EmptyPool_YieldsNothing()
    {
        MysteryBoxPrizePicker.Pick([], MysteryBoxPrizePool.Box, "purple", _ => 0).Should().BeNull();
    }

    [Fact]
    public void PrizesFromAnotherPool_AreNeverDrawn()
    {
        List<MysteryBoxPrizeSnapshot> prizes = [Prize(1, MysteryBoxPrizePool.Trophy)];

        MysteryBoxPrizePicker
            .Pick(prizes, MysteryBoxPrizePool.Box, "purple", _ => 0)
            .Should()
            .BeNull();
    }

    [Fact]
    public void ColourRestrictedPrizes_OnlyDropFromTheirOwnColour()
    {
        List<MysteryBoxPrizeSnapshot> prizes = [Prize(1, color: "blue")];

        MysteryBoxPrizePicker
            .Pick(prizes, MysteryBoxPrizePool.Box, "purple", _ => 0)
            .Should()
            .BeNull();
        MysteryBoxPrizePicker
            .Pick(prizes, MysteryBoxPrizePool.Box, "blue", _ => 0)!
            .Id.Should()
            .Be(1);
    }

    [Fact]
    public void ColourlessPrizes_DropFromEveryColour()
    {
        List<MysteryBoxPrizeSnapshot> prizes = [Prize(1)];

        MysteryBoxPrizePicker
            .Pick(prizes, MysteryBoxPrizePool.Box, "turquoise", _ => 0)!
            .Id.Should()
            .Be(1);
    }

    [Fact]
    public void ColourMatching_IgnoresCasingAndPadding()
    {
        List<MysteryBoxPrizeSnapshot> prizes = [Prize(1, color: "red")];

        MysteryBoxPrizePicker
            .Pick(prizes, MysteryBoxPrizePool.Box, "  RED  ", _ => 0)!
            .Id.Should()
            .Be(1);
    }

    [Fact]
    public void NonPositiveWeights_AreExcludedFromTheDraw()
    {
        List<MysteryBoxPrizeSnapshot> prizes = [Prize(1, weight: 0), Prize(2, weight: -5)];

        MysteryBoxPrizePicker
            .Pick(prizes, MysteryBoxPrizePool.Box, "purple", _ => 0)
            .Should()
            .BeNull();
    }

    [Theory]
    // Weights 1 / 3 / 6 carve [0,1) [1,4) [4,10); every boundary must land in the right bucket.
    [InlineData(0, 1)]
    [InlineData(1, 2)]
    [InlineData(3, 2)]
    [InlineData(4, 3)]
    [InlineData(9, 3)]
    public void WeightsCarveContiguousRanges(int roll, int expectedId)
    {
        List<MysteryBoxPrizeSnapshot> prizes =
        [
            Prize(1, weight: 1),
            Prize(2, weight: 3),
            Prize(3, weight: 6),
        ];

        int seenTotal = -1;

        MysteryBoxPrizeSnapshot? picked = MysteryBoxPrizePicker.Pick(
            prizes,
            MysteryBoxPrizePool.Box,
            "purple",
            total =>
            {
                seenTotal = total;
                return roll;
            }
        );

        seenTotal.Should().Be(10);
        picked!.Id.Should().Be(expectedId);
    }

    [Fact]
    public void TotalWeightExcludesIneligibleRows()
    {
        List<MysteryBoxPrizeSnapshot> prizes =
        [
            Prize(1, weight: 2),
            Prize(2, color: "blue", weight: 50),
            Prize(3, pool: MysteryBoxPrizePool.Trophy, weight: 50),
        ];

        int seenTotal = -1;

        MysteryBoxPrizePicker.Pick(
            prizes,
            MysteryBoxPrizePool.Box,
            "purple",
            total =>
            {
                seenTotal = total;
                return 0;
            }
        );

        seenTotal.Should().Be(2);
    }

    [Fact]
    public void OutOfRangeRoll_Throws_RatherThanReadingAsAnEmptyPool()
    {
        List<MysteryBoxPrizeSnapshot> prizes = [Prize(1, weight: 3)];

        Action pick = () =>
            MysteryBoxPrizePicker.Pick(prizes, MysteryBoxPrizePool.Box, "purple", _ => 3);

        pick.Should().Throw<ArgumentOutOfRangeException>();
    }
}
