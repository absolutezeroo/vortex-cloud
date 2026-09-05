using FluentAssertions;
using Vortex.RewardTracks.Progression;
using Xunit;

namespace Vortex.Rewards.Tests;

/// <summary>
/// The premium multiplier. Small, and worth pinning to the last point: it decides how much every
/// premium player is paid for every stage they ever complete.
/// </summary>
public class PremiumBoostTests
{
    [Fact]
    public void The_brief_example_holds_exactly()
    {
        PremiumBoost
            .Apply(25, Content.Premium(boostPerMille: 1200), premiumActive: true)
            .Should()
            .Be(30);
    }

    [Fact]
    public void Rounding_is_half_up()
    {
        // 25 x 1.15 = 28.75. Rounding down would quietly shave a point off most grants, which reads
        // to a player as the boost not working.
        PremiumBoost
            .Apply(25, Content.Premium(boostPerMille: 1150), premiumActive: true)
            .Should()
            .Be(29);

        // 10 x 1.15 = 11.5, which rounds away from zero.
        PremiumBoost
            .Apply(10, Content.Premium(boostPerMille: 1150), premiumActive: true)
            .Should()
            .Be(12);

        // 10 x 1.14 = 11.4.
        PremiumBoost
            .Apply(10, Content.Premium(boostPerMille: 1140), premiumActive: true)
            .Should()
            .Be(11);
    }

    [Fact]
    public void Nothing_is_boosted_without_premium()
    {
        PremiumBoost.Apply(25, Content.Premium(), premiumActive: false).Should().Be(25);
    }

    [Fact]
    public void Nothing_is_boosted_on_a_track_with_no_premium_tier()
    {
        PremiumBoost.Apply(25, null, premiumActive: true).Should().Be(25);
    }

    [Fact]
    public void A_multiplier_at_or_below_one_is_treated_as_no_boost()
    {
        PremiumBoost.Apply(25, Content.Premium(boostPerMille: 1000), true).Should().Be(25);

        // Content nobody meant to write. Charging a player for slower progress is the worse answer.
        PremiumBoost.Apply(25, Content.Premium(boostPerMille: 800), true).Should().Be(25);
    }

    [Fact]
    public void Zero_stays_zero()
    {
        PremiumBoost.Apply(0, Content.Premium(), true).Should().Be(0);
    }

    /// <summary>
    /// The same input always gives the same answer, whatever the multiplier. Integer arithmetic is
    /// the whole reason: a double would give 29 or 30 for the same content depending on how the
    /// operator's decimal rounded on the way in.
    /// </summary>
    [Theory]
    [InlineData(1, 1200, 1)]
    [InlineData(3, 1200, 4)]
    [InlineData(7, 1333, 9)]
    [InlineData(40, 1250, 50)]
    [InlineData(999, 1001, 1000)]
    public void The_answer_is_deterministic(int basePoints, int perMille, int expected)
    {
        PremiumBoost
            .Apply(basePoints, Content.Premium(boostPerMille: perMille), true)
            .Should()
            .Be(expected);
    }
}
