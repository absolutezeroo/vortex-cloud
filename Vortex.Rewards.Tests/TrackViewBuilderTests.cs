using System.Linq;
using FluentAssertions;
using Vortex.Primitives.RewardTracks;
using Vortex.Primitives.RewardTracks.Snapshots;
using Vortex.RewardTracks.Progression;
using Xunit;

namespace Vortex.Rewards.Tests;

/// <summary>
/// The fold of a definition and a player's state into what the client is sent. The three states a
/// prize can be in are decided here, and the client draws a different thing for each.
/// </summary>
public class TrackViewBuilderTests
{
    [Fact]
    public void A_prize_below_the_threshold_is_locked()
    {
        RewardTrackViewSnapshot view = TrackViewBuilder.Build(
            Content.Track(prizes: [Content.Prize("p1", 100)]),
            Content.State(points: 40)
        );

        RewardTrackPrizeViewSnapshot prize = view.Prizes.Single();

        prize.Available.Should().BeFalse();
        prize.Claimed.Should().BeFalse();
        prize.Claimable.Should().BeFalse();
    }

    [Fact]
    public void A_prize_at_the_threshold_is_available_and_unclaimed()
    {
        RewardTrackViewSnapshot view = TrackViewBuilder.Build(
            Content.Track(prizes: [Content.Prize("p1", 100)]),
            Content.State(points: 100)
        );

        RewardTrackPrizeViewSnapshot prize = view.Prizes.Single();

        prize.Available.Should().BeTrue();
        prize.Claimed.Should().BeFalse();
        prize.Claimable.Should().BeTrue("available and claimed are different states");
    }

    [Fact]
    public void A_claimed_prize_stays_available_and_is_marked_claimed()
    {
        RewardTrackViewSnapshot view = TrackViewBuilder.Build(
            Content.Track(prizes: [Content.Prize("p1", 100)]),
            Content.State(points: 120, claimed: ["p1"])
        );

        RewardTrackPrizeViewSnapshot prize = view.Prizes.Single();

        prize.Available.Should().BeTrue();
        prize.Claimed.Should().BeTrue();
        prize.Claimable.Should().BeFalse();
    }

    [Fact]
    public void A_premium_prize_is_locked_without_premium_however_many_points_there_are()
    {
        RewardTrackDefinitionSnapshot track = Content.Track(
            premium: Content.Premium(),
            prizes: [Content.Prize("p1", 10, premium: true)]
        );

        TrackViewBuilder
            .Build(track, Content.State(points: 9999))
            .Prizes.Single()
            .Available.Should()
            .BeFalse();

        TrackViewBuilder
            .Build(track, Content.State(points: 10, premiumUnlocked: true))
            .Prizes.Single()
            .Available.Should()
            .BeTrue();
    }

    /// <summary>
    /// The two completion booleans, matching the client's own derivation: free completion is every
    /// non-premium prize claimed, and premium completion needs both halves — or is trivially true on
    /// a track with no premium tier.
    /// </summary>
    [Fact]
    public void Complete_and_premium_complete_follow_the_clients_own_rule()
    {
        RewardTrackDefinitionSnapshot track = Content.Track(
            premium: Content.Premium(),
            prizes: [Content.Prize("free", 10), Content.Prize("prem", 10, premium: true)]
        );

        RewardTrackViewSnapshot none = TrackViewBuilder.Build(track, Content.State(points: 10));

        none.Complete.Should().BeFalse();
        none.PremiumComplete.Should().BeFalse();

        RewardTrackViewSnapshot freeOnly = TrackViewBuilder.Build(
            track,
            Content.State(points: 10, claimed: ["free"])
        );

        freeOnly.Complete.Should().BeTrue();
        freeOnly.PremiumComplete.Should().BeFalse();

        RewardTrackViewSnapshot both = TrackViewBuilder.Build(
            track,
            Content.State(points: 10, premiumUnlocked: true, claimed: ["free", "prem"])
        );

        both.Complete.Should().BeTrue();
        both.PremiumComplete.Should().BeTrue();
    }

    [Fact]
    public void A_track_with_no_premium_tier_is_premium_complete_by_definition()
    {
        RewardTrackViewSnapshot view = TrackViewBuilder.Build(
            Content.Track(prizes: [Content.Prize("free", 10)]),
            Content.State(points: 10, claimed: ["free"])
        );

        view.PremiumComplete.Should().BeTrue();
    }

    /// <summary>
    /// A premium-only task is still sent to a free player, locked. Hiding it would hide the offer,
    /// which is half of what the premium tier is for.
    /// </summary>
    [Fact]
    public void A_premium_task_is_sent_to_a_free_player()
    {
        RewardTrackViewSnapshot view = TrackViewBuilder.Build(
            Content.Track(tasks: [Content.Task("t", premium: true)]),
            Content.State()
        );

        view.Tasks.Should().ContainSingle().Which.Premium.Should().BeTrue();
    }

    [Fact]
    public void A_task_the_player_has_never_touched_reports_zero()
    {
        RewardTrackViewSnapshot view = TrackViewBuilder.Build(
            Content.Track(tasks: [Content.Task("t")]),
            Content.State()
        );

        view.Tasks.Single().ProgressCount.Should().Be(0);
    }

    /// <summary>The first reward of a bundle is what the client draws for the prize.</summary>
    [Fact]
    public void The_bundles_first_reward_is_the_one_shown()
    {
        RewardTrackViewSnapshot view = TrackViewBuilder.Build(
            Content.Track(
                prizes:
                [
                    Content.Prize(
                        "p1",
                        10,
                        false,
                        Content.Reward(RewardKind.Habbicon, "7"),
                        Content.Reward(RewardKind.Currency, "0", 300)
                    ),
                ]
            ),
            Content.State(points: 10)
        );

        RewardTrackPrizeViewSnapshot prize = view.Prizes.Single();

        prize.Kind.Should().Be(RewardKind.Habbicon);
        prize.RewardTypeId.Should().Be("7");
    }
}
