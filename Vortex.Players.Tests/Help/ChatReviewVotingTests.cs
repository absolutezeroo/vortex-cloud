using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Orleans;
using Vortex.Players.Grains;
using Vortex.Primitives.Help;
using Vortex.Tests.Support;
using Xunit;

namespace Vortex.Players.Tests.Help;

/// <summary>
/// A chat review is judged by several guardians at once, which is what makes it different from a
/// help request: nobody takes ownership, they vote. The rules worth pinning are who gets asked, when
/// it is finished, and what a tie means.
/// </summary>
public sealed class ChatReviewVotingTests
{
    private const int Reporter = 100;
    private const int GuardianA = 1;
    private const int GuardianB = 2;
    private const int GuardianC = 3;

    private const int VoteAcceptable = 0;
    private const int VoteAbusive = 1;

    private static GuideDirectoryGrain NewRoster() => GuideRosterHarness.New();

    private static Task GuardianOnDutyAsync(GuideDirectoryGrain roster, int playerId) =>
        roster.SetDutyAsync(playerId, true, false, false, true, CancellationToken.None);

    [Fact]
    public async Task AReviewIsOfferedToEveryGuardianAtOnce()
    {
        GuideDirectoryGrain roster = NewRoster();
        await GuardianOnDutyAsync(roster, GuardianA);
        await GuardianOnDutyAsync(roster, GuardianB);

        ChatReviewOutcome outcome = await roster.CreateChatReviewAsync(
            Reporter,
            "he said something awful",
            CancellationToken.None
        );

        // All of them, not one: the point is several opinions on the same excerpt.
        outcome.OfferedTo.Should().BeEquivalentTo(new[] { GuardianA, GuardianB });
        outcome.ChatRecord.Should().Be("he said something awful");
    }

    [Fact]
    public async Task GuidesAndHelpersAreNotAskedToJudgeChat()
    {
        GuideDirectoryGrain roster = NewRoster();

        // On duty for tours and help, but not chat reviews.
        await roster.SetDutyAsync(GuardianA, true, true, true, false, CancellationToken.None);

        ChatReviewOutcome outcome = await roster.CreateChatReviewAsync(
            Reporter,
            "chat",
            CancellationToken.None
        );

        outcome.Nothing.Should().BeTrue();
    }

    [Fact]
    public async Task TheReporterNeverJudgesTheirOwnReport()
    {
        GuideDirectoryGrain roster = NewRoster();
        await GuardianOnDutyAsync(roster, Reporter);
        await GuardianOnDutyAsync(roster, GuardianA);

        ChatReviewOutcome outcome = await roster.CreateChatReviewAsync(
            Reporter,
            "chat",
            CancellationToken.None
        );

        outcome.OfferedTo.Should().BeEquivalentTo(new[] { GuardianA });
    }

    [Fact]
    public async Task TheVerdictArrivesOnceEveryoneWhoAcceptedHasVoted()
    {
        GuideDirectoryGrain roster = NewRoster();
        await GuardianOnDutyAsync(roster, GuardianA);
        await GuardianOnDutyAsync(roster, GuardianB);

        await roster.CreateChatReviewAsync(Reporter, "chat", CancellationToken.None);
        await roster.ChatReviewDecideAsync(GuardianA, true, CancellationToken.None);
        await roster.ChatReviewDecideAsync(GuardianB, true, CancellationToken.None);

        ChatReviewOutcome first = await roster.ChatReviewVoteAsync(
            GuardianA,
            VoteAbusive,
            CancellationToken.None
        );

        // One vote in, one outstanding: nothing is decided yet.
        first.Result.Should().BeNull();

        ChatReviewOutcome second = await roster.ChatReviewVoteAsync(
            GuardianB,
            VoteAbusive,
            CancellationToken.None
        );

        second.Result.Should().NotBeNull();
        second.Result!.WinningVote.Should().Be(VoteAbusive);
        second.Result.Votes.Should().HaveCount(2);
    }

    [Fact]
    public async Task DecliningCanBeWhatFinishesIt()
    {
        GuideDirectoryGrain roster = NewRoster();
        await GuardianOnDutyAsync(roster, GuardianA);
        await GuardianOnDutyAsync(roster, GuardianB);

        await roster.CreateChatReviewAsync(Reporter, "chat", CancellationToken.None);
        await roster.ChatReviewDecideAsync(GuardianA, true, CancellationToken.None);
        await roster.ChatReviewVoteAsync(GuardianA, VoteAbusive, CancellationToken.None);

        // B was still deciding, so A's vote could not resolve it. B passing is what does.
        ChatReviewOutcome outcome = await roster.ChatReviewDecideAsync(
            GuardianB,
            false,
            CancellationToken.None
        );

        outcome.Result.Should().NotBeNull();
        outcome.Result!.Votes.Should().HaveCount(1);
    }

    [Fact]
    public async Task ATieReadsAsNotAbusive()
    {
        GuideDirectoryGrain roster = NewRoster();
        await GuardianOnDutyAsync(roster, GuardianA);
        await GuardianOnDutyAsync(roster, GuardianB);

        await roster.CreateChatReviewAsync(Reporter, "chat", CancellationToken.None);
        await roster.ChatReviewDecideAsync(GuardianA, true, CancellationToken.None);
        await roster.ChatReviewDecideAsync(GuardianB, true, CancellationToken.None);
        await roster.ChatReviewVoteAsync(GuardianA, VoteAbusive, CancellationToken.None);

        ChatReviewOutcome outcome = await roster.ChatReviewVoteAsync(
            GuardianB,
            VoteAcceptable,
            CancellationToken.None
        );

        // Condemning a chat needs a majority, not merely the absence of one.
        outcome.Result!.WinningVote.Should().Be(VoteAcceptable);
    }

    [Fact]
    public async Task AMajorityCarriesIt()
    {
        GuideDirectoryGrain roster = NewRoster();
        await GuardianOnDutyAsync(roster, GuardianA);
        await GuardianOnDutyAsync(roster, GuardianB);
        await GuardianOnDutyAsync(roster, GuardianC);

        await roster.CreateChatReviewAsync(Reporter, "chat", CancellationToken.None);

        foreach (int guardian in new[] { GuardianA, GuardianB, GuardianC })
        {
            await roster.ChatReviewDecideAsync(guardian, true, CancellationToken.None);
        }

        await roster.ChatReviewVoteAsync(GuardianA, VoteAbusive, CancellationToken.None);
        await roster.ChatReviewVoteAsync(GuardianB, VoteAcceptable, CancellationToken.None);

        ChatReviewOutcome outcome = await roster.ChatReviewVoteAsync(
            GuardianC,
            VoteAbusive,
            CancellationToken.None
        );

        outcome.Result!.WinningVote.Should().Be(VoteAbusive);
        outcome.Result.VotesByGuardian[GuardianB].Should().Be(VoteAcceptable);
    }

    [Fact]
    public async Task AGuardianMayChangeTheirMindBeforeTheOthersFinish()
    {
        GuideDirectoryGrain roster = NewRoster();
        await GuardianOnDutyAsync(roster, GuardianA);
        await GuardianOnDutyAsync(roster, GuardianB);

        await roster.CreateChatReviewAsync(Reporter, "chat", CancellationToken.None);
        await roster.ChatReviewDecideAsync(GuardianA, true, CancellationToken.None);
        await roster.ChatReviewDecideAsync(GuardianB, true, CancellationToken.None);

        await roster.ChatReviewVoteAsync(GuardianA, VoteAbusive, CancellationToken.None);
        await roster.ChatReviewVoteAsync(GuardianA, VoteAcceptable, CancellationToken.None);

        ChatReviewOutcome outcome = await roster.ChatReviewVoteAsync(
            GuardianB,
            VoteAcceptable,
            CancellationToken.None
        );

        // The change replaced the first vote rather than counting twice.
        outcome.Result!.Votes.Should().HaveCount(2);
        outcome.Result.WinningVote.Should().Be(VoteAcceptable);
    }

    [Fact]
    public async Task EverybodyWalkingAwayProducesNoVerdict()
    {
        GuideDirectoryGrain roster = NewRoster();
        await GuardianOnDutyAsync(roster, GuardianA);

        await roster.CreateChatReviewAsync(Reporter, "chat", CancellationToken.None);

        ChatReviewOutcome outcome = await roster.ChatReviewDecideAsync(
            GuardianA,
            false,
            CancellationToken.None
        );

        // Nothing to report and nothing to send -- a verdict from nobody is not a verdict.
        outcome.Nothing.Should().BeTrue();
    }

    [Fact]
    public async Task AGuardianWhoWalksOutMidReviewIsNotWaitedFor()
    {
        GuideDirectoryGrain roster = NewRoster();
        await GuardianOnDutyAsync(roster, GuardianA);
        await GuardianOnDutyAsync(roster, GuardianB);

        await roster.CreateChatReviewAsync(Reporter, "chat", CancellationToken.None);
        await roster.ChatReviewDecideAsync(GuardianA, true, CancellationToken.None);
        await roster.ChatReviewDecideAsync(GuardianB, true, CancellationToken.None);
        await roster.ChatReviewVoteAsync(GuardianA, VoteAbusive, CancellationToken.None);

        ChatReviewOutcome outcome = await roster.ChatReviewDetachAsync(
            GuardianB,
            CancellationToken.None
        );

        outcome.Result.Should().NotBeNull();
        outcome.Result!.Votes.Should().HaveCount(1);
    }

    [Fact]
    public async Task OnceFinishedTheGuardiansAreFreeForTheNextReview()
    {
        GuideDirectoryGrain roster = NewRoster();
        await GuardianOnDutyAsync(roster, GuardianA);

        await roster.CreateChatReviewAsync(Reporter, "first", CancellationToken.None);
        await roster.ChatReviewDecideAsync(GuardianA, true, CancellationToken.None);
        await roster.ChatReviewVoteAsync(GuardianA, VoteAbusive, CancellationToken.None);

        ChatReviewOutcome next = await roster.CreateChatReviewAsync(
            101,
            "second",
            CancellationToken.None
        );

        next.OfferedTo.Should().BeEquivalentTo(new[] { GuardianA });
    }

    [Fact]
    public async Task AVoteFromSomebodyWhoNeverAcceptedIsIgnored()
    {
        GuideDirectoryGrain roster = NewRoster();
        await GuardianOnDutyAsync(roster, GuardianA);
        await GuardianOnDutyAsync(roster, GuardianB);

        await roster.CreateChatReviewAsync(Reporter, "chat", CancellationToken.None);
        await roster.ChatReviewDecideAsync(GuardianA, true, CancellationToken.None);

        // B was offered it but has not taken it, so their vote is not theirs to cast.
        ChatReviewOutcome stray = await roster.ChatReviewVoteAsync(
            GuardianB,
            VoteAbusive,
            CancellationToken.None
        );

        stray.Nothing.Should().BeTrue();
    }
}
