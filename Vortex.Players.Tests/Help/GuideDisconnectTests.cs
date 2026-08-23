using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Vortex.Primitives.Help;
using Vortex.Social.Grains;
using Xunit;

namespace Vortex.Players.Tests.Help;

/// <summary>
/// What a disconnect has to undo. On duty means available right now, so a closed client cannot leave
/// somebody counted, holding an offer, or paired — each of those strands a real player waiting on an
/// answer that is never coming.
/// </summary>
public sealed class GuideDisconnectTests
{
    private const int Requester = 100;
    private const int GuideA = 1;
    private const int GuideB = 2;

    private static Task OnDutyAsync(GuideDirectoryGrain roster, int playerId) =>
        roster.SetDutyAsync(playerId, true, true, true, true, CancellationToken.None);

    [Fact]
    public async Task ADepartedGuideStopsBeingCounted()
    {
        GuideDirectoryGrain roster = GuideRosterHarness.New();
        await OnDutyAsync(roster, GuideA);
        await OnDutyAsync(roster, GuideB);

        await roster.ClearDutyAsync(GuideA, CancellationToken.None);

        GuideDutySnapshot duty = await roster.GetStatusAsync(GuideB, CancellationToken.None);

        duty.GuidesOnDuty.Should().Be(1);
        duty.HelpersOnDuty.Should().Be(1);
        duty.GuardiansOnDuty.Should().Be(1);
    }

    [Fact]
    public async Task ADepartedGuideIsNoLongerOfferedRequests()
    {
        GuideDirectoryGrain roster = GuideRosterHarness.New();
        await OnDutyAsync(roster, GuideA);

        await roster.ClearDutyAsync(GuideA, CancellationToken.None);

        GuideRequestOutcome outcome = await roster.CreateRequestAsync(
            Requester,
            0,
            "anyone?",
            CancellationToken.None
        );

        // The request must fail honestly rather than sit in front of somebody who has gone. It only
        // moves on when declined, and a departed guide declines nothing.
        outcome.Failed.Should().BeTrue();
    }

    [Fact]
    public async Task LeavingMidSessionReleasesThePartner()
    {
        GuideDirectoryGrain roster = GuideRosterHarness.New();
        await OnDutyAsync(roster, GuideA);

        GuideRequestOutcome offered = await roster.CreateRequestAsync(
            Requester,
            0,
            "help",
            CancellationToken.None
        );
        await roster.GuideDecidesAsync(offered.OfferedGuideId, true, CancellationToken.None);

        // The partner comes back so the caller knows who to tell -- that is what carries the
        // "your guide vanished" reason to them.
        int partner = await roster.EndSessionAsync(GuideA, CancellationToken.None);

        partner.Should().Be(Requester);
        (await roster.GetPartnerAsync(Requester, CancellationToken.None)).Should().Be(0);
    }

    [Fact]
    public async Task LeavingFreesTheRequestSomebodyWasStillHolding()
    {
        GuideDirectoryGrain roster = GuideRosterHarness.New();
        await OnDutyAsync(roster, GuideA);
        await OnDutyAsync(roster, GuideB);

        await roster.CreateRequestAsync(Requester, 0, "help", CancellationToken.None);

        // The requester leaves before anyone accepted; the guide holding the offer must come free.
        await roster.EndSessionAsync(Requester, CancellationToken.None);

        GuideRequestOutcome next = await roster.CreateRequestAsync(
            101,
            0,
            "me next",
            CancellationToken.None
        );

        next.Failed.Should().BeFalse();
    }

    [Fact]
    public async Task LeavingAChatReviewCanBeWhatResolvesIt()
    {
        GuideDirectoryGrain roster = GuideRosterHarness.New();
        await OnDutyAsync(roster, GuideA);
        await OnDutyAsync(roster, GuideB);

        await roster.CreateChatReviewAsync(Requester, "chat", CancellationToken.None);
        await roster.ChatReviewDecideAsync(GuideA, true, CancellationToken.None);
        await roster.ChatReviewDecideAsync(GuideB, true, CancellationToken.None);
        await roster.ChatReviewVoteAsync(GuideA, 1, CancellationToken.None);

        ChatReviewOutcome outcome = await roster.ChatReviewDetachAsync(
            GuideB,
            CancellationToken.None
        );

        // The guardians who did vote get their verdict instead of waiting on somebody who has gone.
        outcome.Result.Should().NotBeNull();
        outcome.Result!.Votes.Should().HaveCount(1);
    }

    [Fact]
    public async Task CleaningUpSomebodyWhoWasInNothingIsHarmless()
    {
        GuideDirectoryGrain roster = GuideRosterHarness.New();

        // Every disconnect runs this, and almost nobody is a guide.
        await roster.ClearDutyAsync(999, CancellationToken.None);

        (await roster.EndSessionAsync(999, CancellationToken.None)).Should().Be(0);
        (await roster.ChatReviewDetachAsync(999, CancellationToken.None)).Nothing.Should().BeTrue();
    }
}
