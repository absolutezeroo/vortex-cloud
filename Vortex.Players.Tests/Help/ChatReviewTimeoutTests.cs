using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Vortex.Players.Grains;
using Vortex.Primitives.Help;
using Xunit;

namespace Vortex.Players.Tests.Help;

/// <summary>
/// Giving up on guardians who stopped answering. Without this a review stays open forever on one
/// person's silence, and everyone else who voted never learns the verdict — which is the shape the
/// subsystem shipped in before the sweep existed.
/// </summary>
/// <remarks>
/// The sweep takes the time rather than reading it, so these move the clock instead of sleeping.
/// </remarks>
public sealed class ChatReviewTimeoutTests
{
    private const int Reporter = 100;
    private const int GuardianA = 1;
    private const int GuardianB = 2;

    // Offsets from the moment the review is opened. The grain stamps it from the machine clock, so
    // the tests read that same clock rather than inventing an absolute time -- the margins are tens
    // of seconds, far wider than the milliseconds between the two reads.
    private const long JustUnderAcceptance = 29_000;
    private const long PastAcceptance = 31_000;
    private const long PastVoting = 121_000;

    private static long Now() => Environment.TickCount64;

    private const int VoteAbusive = 1;

    private static Task GuardianOnDutyAsync(GuideDirectoryGrain roster, int playerId) =>
        roster.SetDutyAsync(playerId, true, false, false, true, CancellationToken.None);

    [Fact]
    public async Task NobodyIsDroppedBeforeTheirDeadline()
    {
        GuideDirectoryGrain roster = GuideRosterHarness.New();
        await GuardianOnDutyAsync(roster, GuardianA);

        long openedAt = Now();
        await roster.CreateChatReviewAsync(Reporter, "chat", CancellationToken.None);

        roster.SweepChatReviewTimeouts(openedAt + JustUnderAcceptance).Should().BeEmpty();
    }

    [Fact]
    public async Task AGuardianWhoNeverAnswersIsGivenUpOn()
    {
        GuideDirectoryGrain roster = GuideRosterHarness.New();
        await GuardianOnDutyAsync(roster, GuardianA);
        await GuardianOnDutyAsync(roster, GuardianB);

        long openedAt = Now();
        await roster.CreateChatReviewAsync(Reporter, "chat", CancellationToken.None);
        await roster.ChatReviewDecideAsync(GuardianA, true, CancellationToken.None);
        await roster.ChatReviewVoteAsync(GuardianA, VoteAbusive, CancellationToken.None);

        // B never answered the offer. A has voted and is owed a verdict.
        List<ChatReviewOutcome> resolved = roster.SweepChatReviewTimeouts(
            openedAt + PastAcceptance
        );

        resolved.Should().HaveCount(1);
        resolved[0].Result.Should().NotBeNull();
        resolved[0].Result!.Votes.Should().HaveCount(1);
    }

    [Fact]
    public async Task AcceptingBuysTheLongerDeadline()
    {
        GuideDirectoryGrain roster = GuideRosterHarness.New();
        await GuardianOnDutyAsync(roster, GuardianA);

        long openedAt = Now();
        await roster.CreateChatReviewAsync(Reporter, "chat", CancellationToken.None);
        await roster.ChatReviewDecideAsync(GuardianA, true, CancellationToken.None);

        // Past the deadline for answering, but they answered -- they are reading, and get the
        // voting deadline instead.
        roster.SweepChatReviewTimeouts(openedAt + PastAcceptance).Should().BeEmpty();
    }

    [Fact]
    public async Task SilenceIsNotAVote()
    {
        GuideDirectoryGrain roster = GuideRosterHarness.New();
        await GuardianOnDutyAsync(roster, GuardianA);
        await GuardianOnDutyAsync(roster, GuardianB);

        long openedAt = Now();
        await roster.CreateChatReviewAsync(Reporter, "chat", CancellationToken.None);
        await roster.ChatReviewDecideAsync(GuardianA, true, CancellationToken.None);
        await roster.ChatReviewDecideAsync(GuardianB, true, CancellationToken.None);
        await roster.ChatReviewVoteAsync(GuardianA, VoteAbusive, CancellationToken.None);

        List<ChatReviewOutcome> resolved = roster.SweepChatReviewTimeouts(openedAt + PastVoting);

        // B is dropped rather than counted: somebody who wandered off must not tip a verdict by
        // doing nothing.
        resolved.Should().HaveCount(1);
        resolved[0].Result!.Votes.Should().HaveCount(1);
        resolved[0].Result!.WinningVote.Should().Be(VoteAbusive);
    }

    [Fact]
    public async Task AReviewNobodyAnsweredProducesNoVerdict()
    {
        GuideDirectoryGrain roster = GuideRosterHarness.New();
        await GuardianOnDutyAsync(roster, GuardianA);

        long openedAt = Now();
        await roster.CreateChatReviewAsync(Reporter, "chat", CancellationToken.None);

        // It is closed, but there is nothing to tell anyone -- a verdict from nobody is not a
        // verdict, so no packet is produced.
        roster.SweepChatReviewTimeouts(openedAt + PastAcceptance).Should().BeEmpty();
    }

    [Fact]
    public async Task AnExpiredReviewFreesItsGuardians()
    {
        GuideDirectoryGrain roster = GuideRosterHarness.New();
        await GuardianOnDutyAsync(roster, GuardianA);

        long openedAt = Now();
        await roster.CreateChatReviewAsync(Reporter, "first", CancellationToken.None);
        roster.SweepChatReviewTimeouts(openedAt + PastAcceptance);

        // The guardian must be offerable again, or one abandoned review would take them out of
        // circulation for good.
        ChatReviewOutcome next = await roster.CreateChatReviewAsync(
            101,
            "second",
            CancellationToken.None
        );

        next.OfferedTo.Should().BeEquivalentTo(new[] { GuardianA });
    }

    [Fact]
    public async Task SweepingAgainAfterEverythingClosedDoesNothing()
    {
        GuideDirectoryGrain roster = GuideRosterHarness.New();
        await GuardianOnDutyAsync(roster, GuardianA);

        long openedAt = Now();
        await roster.CreateChatReviewAsync(Reporter, "chat", CancellationToken.None);
        await roster.ChatReviewDecideAsync(GuardianA, true, CancellationToken.None);
        await roster.ChatReviewVoteAsync(GuardianA, VoteAbusive, CancellationToken.None);

        roster.SweepChatReviewTimeouts(openedAt + PastVoting).Should().BeEmpty();
    }
}
