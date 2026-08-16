using System;
using System.Collections.Generic;
using FluentAssertions;
using Vortex.Rooms.Wired;
using Xunit;

namespace Vortex.Rooms.Tests.Wired;

/// <summary>
/// The random add-on's draw. Its second slider — "avoid effects from the last N executions" — is a
/// preference, and the failure that matters is a pile that stops firing because every effect it has
/// is on the avoid list.
/// </summary>
public sealed class WiredRandomEffectPickerTests
{
    private static readonly Random Rng = new(1);

    [Fact]
    public void PicksAsManyAsAsked()
    {
        List<int> picked = WiredRandomEffectPicker.Pick([10, 20, 30, 40], 2, Empty, Rng);

        picked.Should().HaveCount(2);
        picked.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void AskingForMoreThanThereAre_TakesThemAll()
    {
        WiredRandomEffectPicker.Pick([10, 20], 5, Empty, Rng).Should().Equal(0, 1);
    }

    [Fact]
    public void TheDrawKeepsThePilesOwnOrder()
    {
        // Indices come back sorted, so the effects run in the order the pile has them, not in the
        // order they were drawn.
        List<int> picked = WiredRandomEffectPicker.Pick([10, 20, 30, 40], 3, Empty, Rng);

        picked.Should().BeInAscendingOrder();
    }

    [Fact]
    public void RecentlyUsedEffects_AreAvoided()
    {
        for (int i = 0; i < 30; i++)
        {
            WiredRandomEffectPicker
                .Pick([10, 20, 30], 1, new HashSet<int> { 10, 20 }, Rng)
                .Should()
                .Equal(2);
        }
    }

    [Fact]
    public void WhenEverythingIsRecent_ThePileStillFires()
    {
        // Dropping the preference beats dropping the firing: a pile that ran all its effects last
        // time would otherwise go silent forever.
        WiredRandomEffectPicker
            .Pick([10, 20], 1, new HashSet<int> { 10, 20 }, Rng)
            .Should()
            .HaveCount(1);
    }

    [Fact]
    public void NothingToPickFrom_PicksNothing()
    {
        WiredRandomEffectPicker.Pick([], 3, Empty, Rng).Should().BeEmpty();
        WiredRandomEffectPicker.Pick([10], 0, Empty, Rng).Should().BeEmpty();
    }

    private static IReadOnlySet<int> Empty => new HashSet<int>();
}
