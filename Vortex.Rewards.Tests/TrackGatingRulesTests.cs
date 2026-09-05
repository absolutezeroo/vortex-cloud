using System;
using FluentAssertions;
using Vortex.Primitives.RewardTracks;
using Vortex.Primitives.RewardTracks.Snapshots;
using Vortex.RewardTracks.Progression;
using Xunit;

namespace Vortex.Rewards.Tests;

/// <summary>Who may see a track, and when it counts as finished.</summary>
public class TrackGatingRulesTests
{
    [Fact]
    public void An_always_track_is_open_to_everyone()
    {
        TrackGatingRules.IsUnlocked(Content.Track(), Content.Facts().Build()).Should().BeTrue();
    }

    [Fact]
    public void A_sequential_chapter_waits_for_the_one_before_it()
    {
        RewardTrackDefinitionSnapshot chapter2 = Content.Track(
            "chapter2",
            unlockKind: RewardTrackUnlockKind.TrackCompleted,
            unlockValue: "chapter1"
        );

        TrackGatingRules.IsUnlocked(chapter2, Content.Facts().Build()).Should().BeFalse();

        TrackGatingRules
            .IsUnlocked(chapter2, Content.Facts().Completed("chapter1").Build())
            .Should()
            .BeTrue();
    }

    [Fact]
    public void A_prize_unlock_names_a_track_and_a_prize()
    {
        RewardTrackDefinitionSnapshot track = Content.Track(
            unlockKind: RewardTrackUnlockKind.PrizeClaimed,
            unlockValue: "intro:intro_free_5"
        );

        TrackGatingRules
            .IsUnlocked(track, Content.Facts().Claimed("intro", "intro_free_1").Build())
            .Should()
            .BeFalse();

        TrackGatingRules
            .IsUnlocked(track, Content.Facts().Claimed("intro", "intro_free_5").Build())
            .Should()
            .BeTrue();
    }

    [Fact]
    public void A_badge_unlock_checks_the_badge()
    {
        RewardTrackDefinitionSnapshot track = Content.Track(
            unlockKind: RewardTrackUnlockKind.BadgeOwned,
            unlockValue: "ACH_Foo"
        );

        TrackGatingRules.IsUnlocked(track, Content.Facts().Build()).Should().BeFalse();
        TrackGatingRules
            .IsUnlocked(track, Content.Facts().Badge("ACH_Foo").Build())
            .Should()
            .BeTrue();
    }

    [Fact]
    public void An_account_age_unlock_compares_days()
    {
        RewardTrackDefinitionSnapshot track = Content.Track(
            unlockKind: RewardTrackUnlockKind.AccountAgeDays,
            unlockValue: "30"
        );

        TrackGatingRules.IsUnlocked(track, Content.Facts().AgeDays(29).Build()).Should().BeFalse();
        TrackGatingRules.IsUnlocked(track, Content.Facts().AgeDays(30).Build()).Should().BeTrue();
    }

    /// <summary>
    /// An unlock value that is not a number must lock the track, not open it. Content written for a
    /// newer server has to fail closed on an older one.
    /// </summary>
    [Fact]
    public void A_malformed_account_age_locks_the_track()
    {
        RewardTrackDefinitionSnapshot track = Content.Track(
            unlockKind: RewardTrackUnlockKind.AccountAgeDays,
            unlockValue: "soon"
        );

        TrackGatingRules
            .IsUnlocked(track, Content.Facts().AgeDays(99999).Build())
            .Should()
            .BeFalse();
    }

    [Fact]
    public void A_feature_flag_unlock_reads_the_flag()
    {
        RewardTrackDefinitionSnapshot track = Content.Track(
            unlockKind: RewardTrackUnlockKind.FeatureFlag,
            unlockValue: "campaign.summer.on"
        );

        TrackGatingRules.IsUnlocked(track, Content.Facts().Build()).Should().BeFalse();
        TrackGatingRules
            .IsUnlocked(track, Content.Facts().Flag("campaign.summer.on", false).Build())
            .Should()
            .BeFalse();
        TrackGatingRules
            .IsUnlocked(track, Content.Facts().Flag("campaign.summer.on", true).Build())
            .Should()
            .BeTrue();
    }

    [Fact]
    public void All_free_prizes_claimed_completes_a_track()
    {
        RewardTrackDefinitionSnapshot track = Content.Track(
            prizes: [Content.Prize("a", 10), Content.Prize("b", 20)]
        );

        Complete(track, Content.State(points: 20, claimed: ["a"])).Should().BeFalse();
        Complete(track, Content.State(points: 20, claimed: ["a", "b"])).Should().BeTrue();
    }

    [Fact]
    public void All_prizes_claimed_needs_the_premium_half_too()
    {
        RewardTrackDefinitionSnapshot track = Content.Track(
            premium: Content.Premium(),
            completion: RewardTrackCompletionPolicy.AllPrizesClaimed,
            prizes: [Content.Prize("a", 10), Content.Prize("p", 10, premium: true)]
        );

        Complete(track, Content.State(points: 10, claimed: ["a"])).Should().BeFalse();
        Complete(track, Content.State(points: 10, premiumUnlocked: true, claimed: ["a", "p"]))
            .Should()
            .BeTrue();
    }

    [Fact]
    public void Max_points_reached_ignores_whether_anything_was_claimed()
    {
        RewardTrackDefinitionSnapshot track = Content.Track(
            completion: RewardTrackCompletionPolicy.MaxPointsReached,
            prizes: [Content.Prize("a", 10), Content.Prize("b", 100)]
        );

        Complete(track, Content.State(points: 99)).Should().BeFalse();
        Complete(track, Content.State(points: 100)).Should().BeTrue();
    }

    [Fact]
    public void All_tasks_completed_ignores_a_premium_task_a_free_player_cannot_reach()
    {
        RewardTrackDefinitionSnapshot track = Content.Track(
            completion: RewardTrackCompletionPolicy.AllTasksCompleted,
            tasks:
            [
                Content.Task("free", levels: [Content.Level(0, 3, 10)]),
                Content.Task("prem", premium: true, levels: [Content.Level(0, 1, 10)]),
            ]
        );

        Complete(track, Content.State(tasks: [Content.Progress("free", 2)])).Should().BeFalse();
        Complete(track, Content.State(tasks: [Content.Progress("free", 3)])).Should().BeTrue();

        // With premium bought, the premium task counts again.
        Complete(track, Content.State(premiumUnlocked: true, tasks: [Content.Progress("free", 3)]))
            .Should()
            .BeFalse();
    }

    /// <summary>
    /// A track with no prizes is never complete. Otherwise an empty draft would fire a completion
    /// event for every player who could see it and unlock every chapter behind it.
    /// </summary>
    [Fact]
    public void An_empty_track_is_never_complete()
    {
        Complete(Content.Track(), Content.State(points: 9999)).Should().BeFalse();
    }

    [Fact]
    public void A_scheduled_track_accepts_neither_progress_nor_claims_before_it_starts()
    {
        DateTime now = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);

        RewardTrackDefinitionSnapshot track = Content.Track(startsAt: now.AddDays(1));

        track.AcceptsProgressAt(now).Should().BeFalse();
        track.AcceptsClaimsAt(now).Should().BeFalse();
        track.AcceptsProgressAt(now.AddDays(2)).Should().BeTrue();
    }

    /// <summary>
    /// The whole reason the two windows are separate: a campaign can stop counting on its last day
    /// and still let people collect what they earned.
    /// </summary>
    [Fact]
    public void Claims_can_stay_open_after_progress_closes()
    {
        DateTime now = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);

        RewardTrackDefinitionSnapshot track = Content.Track(
            progressEndsAt: now.AddDays(-1),
            claimEndsAt: now.AddDays(7)
        );

        track.AcceptsProgressAt(now).Should().BeFalse();
        track.AcceptsClaimsAt(now).Should().BeTrue();
        track.IsVisibleAt(now).Should().BeTrue("a track being claimed from is still shown");
    }

    [Fact]
    public void A_draft_is_invisible_and_an_archived_track_is_too()
    {
        DateTime now = DateTime.UtcNow;

        Content.Track(status: RewardTrackStatus.Draft).IsVisibleAt(now).Should().BeFalse();
        Content.Track(status: RewardTrackStatus.Archived).IsVisibleAt(now).Should().BeFalse();
        Content.Track(status: RewardTrackStatus.Ended).IsVisibleAt(now).Should().BeTrue();
    }

    private static bool Complete(
        RewardTrackDefinitionSnapshot track,
        PlayerRewardTrackStateSnapshot state
    ) => TrackGatingRules.IsComplete(track, TrackViewBuilder.Build(track, state), state);
}
