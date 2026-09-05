using System.Collections.Immutable;
using System.Linq;
using FluentAssertions;
using Vortex.Primitives.RewardTracks;
using Vortex.Primitives.RewardTracks.Admin;
using Vortex.Primitives.RewardTracks.Snapshots;
using Vortex.RewardTracks.Content;
using Xunit;

namespace Vortex.Rewards.Tests;

/// <summary>
/// The content validator. Each of these describes a campaign that would look fine in the dashboard
/// and be unplayable in the hotel.
/// </summary>
public class RewardTrackContentValidatorTests
{
    [Fact]
    public void A_well_formed_track_reports_nothing()
    {
        RewardTrackContentReport report = Validate(
            Content.Track(
                tasks: [Content.Task("t", levels: [Content.Level(0, 5, 50)])],
                prizes: [Content.Prize("p", 50)]
            )
        );

        report.IsValid.Should().BeTrue(string.Join(", ", report.Problems.Select(p => p.Code)));
    }

    /// <summary>The one that matters most: a milestone nobody can reach, however long they play.</summary>
    [Fact]
    public void A_prize_needing_more_points_than_the_track_can_pay_is_reported()
    {
        Codes(
                Content.Track(
                    tasks: [Content.Task("t", levels: [Content.Level(0, 1, 10)])],
                    prizes: [Content.Prize("p", 500)]
                )
            )
            .Should()
            .Contain("prize_unreachable");
    }

    /// <summary>
    /// A premium prize is measured against the premium ceiling, which includes premium stages and
    /// the instant points — so a milestone reachable only with premium is not an error.
    /// </summary>
    [Fact]
    public void A_premium_prize_is_measured_against_the_premium_ceiling()
    {
        Codes(
                Content.Track(
                    premium: Content.Premium(instantPoints: 100),
                    tasks: [Content.Task("t", levels: [Content.Level(0, 1, 10)])],
                    prizes: [Content.Prize("p", 110, premium: true)]
                )
            )
            .Should()
            .NotContain("prize_unreachable");
    }

    [Fact]
    public void Premium_content_on_a_track_that_cannot_be_upgraded_is_reported()
    {
        Codes(
                Content.Track(
                    tasks: [Content.Task("t", levels: [Content.Level(0, 1, 10)])],
                    prizes: [Content.Prize("p", 10, premium: true)]
                )
            )
            .Should()
            .Contain("premium_content_without_premium");
    }

    [Fact]
    public void Premium_priced_at_nothing_is_reported()
    {
        Codes(
                Content.Track(
                    premium: Content.Premium(costCredits: 0, costDiamonds: 0, instantPoints: 10)
                )
            )
            .Should()
            .Contain("premium_free");
    }

    [Fact]
    public void A_task_with_no_stages_is_reported()
    {
        RewardTrackTaskDefinitionSnapshot task = Content.Task("t") with
        {
            Levels = ImmutableArray<RewardTrackTaskLevelSnapshot>.Empty,
        };

        Codes(Content.Track(tasks: [task])).Should().Contain("task_without_levels");
    }

    [Fact]
    public void Stages_that_do_not_ascend_are_reported()
    {
        Codes(
                Content.Track(
                    tasks:
                    [
                        Content.Task(
                            "t",
                            levels: [Content.Level(0, 10, 10), Content.Level(1, 5, 20)]
                        ),
                    ]
                )
            )
            .Should()
            .Contain("levels_not_ascending");
    }

    [Fact]
    public void A_stage_requiring_nothing_is_reported()
    {
        Codes(Content.Track(tasks: [Content.Task("t", levels: [Content.Level(0, 0, 10)])]))
            .Should()
            .Contain("level_requires_nothing");
    }

    /// <summary>
    /// A distinct task counts distinct targets; pinning it to one means it can only ever reach 1.
    /// </summary>
    [Fact]
    public void A_pinned_distinct_task_is_reported()
    {
        Codes(
                Content.Track(
                    tasks:
                    [
                        Content.Task(
                            "t",
                            parameter: "room-7",
                            mode: TaskProgressMode.Distinct,
                            levels: [Content.Level(0, 5, 10)]
                        ),
                    ]
                )
            )
            .Should()
            .Contain("distinct_task_pinned");
    }

    [Fact]
    public void A_prize_that_hands_over_nothing_is_reported()
    {
        RewardTrackPrizeDefinitionSnapshot prize = Content.Prize("p", 0) with
        {
            Rewards = ImmutableArray<RewardGrantSnapshot>.Empty,
        };

        Codes(Content.Track(prizes: [prize])).Should().Contain("prize_without_rewards");
    }

    [Fact]
    public void A_furniture_reward_naming_something_that_is_not_an_id_is_reported()
    {
        Codes(
                Content.Track(
                    prizes:
                    [
                        Content.Prize(
                            "p",
                            0,
                            false,
                            Content.Reward(RewardKind.FloorItem, "a_nice_sofa")
                        ),
                    ]
                )
            )
            .Should()
            .Contain("reward_target_not_numeric");
    }

    /// <summary>A badge code is a string by design, so it is not held to the numeric rule.</summary>
    [Fact]
    public void A_badge_reward_may_name_a_code()
    {
        Codes(
                Content.Track(
                    prizes:
                    [
                        Content.Prize("p", 0, false, Content.Reward(RewardKind.Badge, "ACH_Foo")),
                    ]
                )
            )
            .Should()
            .NotContain("reward_target_not_numeric");
    }

    [Fact]
    public void Duplicate_task_and_prize_ids_are_reported()
    {
        RewardTrackContentReport report = Validate(
            Content.Track(
                tasks: [Content.Task("same"), Content.Task("same")],
                prizes: [Content.Prize("same", 0), Content.Prize("same", 0)]
            )
        );

        report.Problems.Select(p => p.Code).Should().Contain("duplicate_task_id");
        report.Problems.Select(p => p.Code).Should().Contain("duplicate_prize_id");
    }

    [Fact]
    public void Claims_closing_before_progress_does_is_reported()
    {
        System.DateTime now = new(2026, 6, 1, 0, 0, 0, System.DateTimeKind.Utc);

        Codes(
                Content.Track(
                    startsAt: now,
                    progressEndsAt: now.AddDays(30),
                    claimEndsAt: now.AddDays(20)
                )
            )
            .Should()
            .Contain("claims_close_before_progress");
    }

    [Fact]
    public void A_chapter_unlocking_from_a_track_that_does_not_exist_is_reported()
    {
        RewardTrackContentReport report = RewardTrackContentValidator.Validate([
            Content.Track(
                "chapter2",
                unlockKind: RewardTrackUnlockKind.TrackCompleted,
                unlockValue: "chapter1"
            ),
        ]);

        report.Problems.Select(p => p.Code).Should().Contain("unlock_track_missing");
    }

    /// <summary>
    /// Two chapters each waiting on the other. Each looks fine on its own, and the set is
    /// unplayable — which is exactly what a per-track check would miss.
    /// </summary>
    [Fact]
    public void A_cycle_of_chapters_is_reported()
    {
        RewardTrackContentReport report = RewardTrackContentValidator.Validate([
            Content.Track("a", unlockKind: RewardTrackUnlockKind.TrackCompleted, unlockValue: "b"),
            Content.Track("b", unlockKind: RewardTrackUnlockKind.TrackCompleted, unlockValue: "a"),
        ]);

        report.Problems.Select(p => p.Code).Should().Contain("unlock_cycle");
    }

    [Fact]
    public void A_proper_chain_of_chapters_is_fine()
    {
        RewardTrackContentReport report = RewardTrackContentValidator.Validate([
            Content.Track("a"),
            Content.Track("b", unlockKind: RewardTrackUnlockKind.TrackCompleted, unlockValue: "a"),
            Content.Track("c", unlockKind: RewardTrackUnlockKind.TrackCompleted, unlockValue: "b"),
        ]);

        report.Problems.Select(p => p.Code).Should().NotContain("unlock_cycle");
    }

    [Fact]
    public void Two_tracks_sharing_a_content_id_are_reported()
    {
        RewardTrackContentReport report = RewardTrackContentValidator.Validate([
            Content.Track("same"),
            Content.Track("same"),
        ]);

        report.Problems.Select(p => p.Code).Should().Contain("duplicate_track_id");
    }

    private static RewardTrackContentReport Validate(RewardTrackDefinitionSnapshot track) =>
        RewardTrackContentValidator.Validate([track]);

    private static string[] Codes(RewardTrackDefinitionSnapshot track) =>
        [.. Validate(track).Problems.Select(p => p.Code)];
}
