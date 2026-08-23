using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Orleans;
using Vortex.Primitives.Help;
using Vortex.Social.Grains;
using Vortex.Tests.Support;
using Xunit;

namespace Vortex.Players.Tests.Help;

/// <summary>
/// Matching a help request to a guide. Everything here is about who may end up holding a request:
/// one guide at a time, never the person who asked, never someone already busy, and never twice the
/// same guide who has already said no.
/// </summary>
public sealed class GuideMatchingTests
{
    private const int Requester = 100;
    private const int GuideA = 1;
    private const int GuideB = 2;

    private const int TourRequest = 0;
    private const int HelpRequest = 1;

    private static GuideDirectoryGrain NewRoster() => GuideRosterHarness.New();

    private static Task OnDutyAsync(
        GuideDirectoryGrain roster,
        int playerId,
        bool guide = true,
        bool helper = true
    ) => roster.SetDutyAsync(playerId, true, guide, helper, false, CancellationToken.None);

    [Fact]
    public async Task ARequestWithNobodyOnDutyFailsWithTheClientsRejectedCode()
    {
        GuideRequestOutcome outcome = await NewRoster()
            .CreateRequestAsync(Requester, TourRequest, "tour please", CancellationToken.None);

        outcome.Failed.Should().BeTrue();

        // The client subtracts one before switching, so 1 is its "rejected" branch. Zero would fall
        // through to a default that tells the player nothing at all.
        outcome.ErrorCode.Should().Be(1);
        outcome.RequesterId.Should().Be(Requester);
    }

    [Fact]
    public async Task ARequestGoesToOneGuideAndCarriesWhatItIsAbout()
    {
        GuideDirectoryGrain roster = NewRoster();
        await OnDutyAsync(roster, GuideA);
        await OnDutyAsync(roster, GuideB);

        GuideRequestOutcome outcome = await roster.CreateRequestAsync(
            Requester,
            TourRequest,
            "show me around",
            CancellationToken.None
        );

        // One guide, not both: broadcasting would have them race, and the losers would be left
        // dismissing a request somebody else already took.
        outcome.OfferedGuideId.Should().BeOneOf(GuideA, GuideB);
        outcome.Description.Should().Be("show me around");
        outcome.HelpRequestType.Should().Be(TourRequest);
        outcome.Failed.Should().BeFalse();
    }

    [Fact]
    public async Task DecliningPassesTheRequestOnRatherThanEndingIt()
    {
        GuideDirectoryGrain roster = NewRoster();
        await OnDutyAsync(roster, GuideA);
        await OnDutyAsync(roster, GuideB);

        GuideRequestOutcome first = await roster.CreateRequestAsync(
            Requester,
            TourRequest,
            "hello",
            CancellationToken.None
        );

        GuideRequestOutcome second = await roster.GuideDecidesAsync(
            first.OfferedGuideId,
            accepted: false,
            CancellationToken.None
        );

        second.Failed.Should().BeFalse();
        second.OfferedGuideId.Should().NotBe(first.OfferedGuideId);
        second.OfferedGuideId.Should().BeOneOf(GuideA, GuideB);
    }

    [Fact]
    public async Task ARequestEveryGuideDeclinesFails()
    {
        GuideDirectoryGrain roster = NewRoster();
        await OnDutyAsync(roster, GuideA);
        await OnDutyAsync(roster, GuideB);

        GuideRequestOutcome first = await roster.CreateRequestAsync(
            Requester,
            TourRequest,
            "hello",
            CancellationToken.None
        );
        GuideRequestOutcome second = await roster.GuideDecidesAsync(
            first.OfferedGuideId,
            false,
            CancellationToken.None
        );
        GuideRequestOutcome third = await roster.GuideDecidesAsync(
            second.OfferedGuideId,
            false,
            CancellationToken.None
        );

        // Nobody is asked twice, so the request runs out of guides rather than circling.
        third.Failed.Should().BeTrue();
        third.RequesterId.Should().Be(Requester);
    }

    [Fact]
    public async Task AcceptingPairsBothSides()
    {
        GuideDirectoryGrain roster = NewRoster();
        await OnDutyAsync(roster, GuideA);

        GuideRequestOutcome offered = await roster.CreateRequestAsync(
            Requester,
            HelpRequest,
            "I am stuck",
            CancellationToken.None
        );

        GuideRequestOutcome accepted = await roster.GuideDecidesAsync(
            offered.OfferedGuideId,
            true,
            CancellationToken.None
        );

        accepted.Session.Should().NotBeNull();
        accepted.Session!.RequesterId.Should().Be(Requester);
        accepted.Session.GuideId.Should().Be(GuideA);
        accepted.Session.Description.Should().Be("I am stuck");

        (await roster.GetSessionAsync(Requester, CancellationToken.None)).Should().NotBeNull();
        (await roster.GetSessionAsync(GuideA, CancellationToken.None)).Should().NotBeNull();
    }

    [Fact]
    public async Task AGuideAlreadyInASessionIsNotOfferedAnother()
    {
        GuideDirectoryGrain roster = NewRoster();
        await OnDutyAsync(roster, GuideA);

        GuideRequestOutcome first = await roster.CreateRequestAsync(
            Requester,
            TourRequest,
            "one",
            CancellationToken.None
        );
        await roster.GuideDecidesAsync(first.OfferedGuideId, true, CancellationToken.None);

        GuideRequestOutcome second = await roster.CreateRequestAsync(
            101,
            TourRequest,
            "two",
            CancellationToken.None
        );

        second.Failed.Should().BeTrue();
    }

    [Fact]
    public async Task AGuideIsNeverOfferedTheirOwnRequest()
    {
        GuideDirectoryGrain roster = NewRoster();
        await OnDutyAsync(roster, GuideA);

        GuideRequestOutcome outcome = await roster.CreateRequestAsync(
            GuideA,
            TourRequest,
            "guiding myself",
            CancellationToken.None
        );

        outcome.Failed.Should().BeTrue();
    }

    [Fact]
    public async Task ASecondRequestFromTheSamePlayerIsRefused()
    {
        GuideDirectoryGrain roster = NewRoster();
        await OnDutyAsync(roster, GuideA);
        await OnDutyAsync(roster, GuideB);

        await roster.CreateRequestAsync(Requester, TourRequest, "one", CancellationToken.None);

        // Otherwise a resend puts a second copy in front of a second guide, and the two sessions
        // race to attach to one person.
        GuideRequestOutcome second = await roster.CreateRequestAsync(
            Requester,
            TourRequest,
            "one again",
            CancellationToken.None
        );

        second.Failed.Should().BeTrue();
    }

    [Fact]
    public async Task TheQueuesAreSeparate()
    {
        GuideDirectoryGrain roster = NewRoster();

        // Covers tours only.
        await roster.SetDutyAsync(GuideA, true, true, false, false, CancellationToken.None);

        GuideRequestOutcome help = await roster.CreateRequestAsync(
            Requester,
            HelpRequest,
            "help me",
            CancellationToken.None
        );

        help.Failed.Should().BeTrue();

        GuideRequestOutcome tour = await roster.CreateRequestAsync(
            101,
            TourRequest,
            "tour me",
            CancellationToken.None
        );

        tour.OfferedGuideId.Should().Be(GuideA);
    }

    [Fact]
    public async Task AnAnswerFromAGuideHoldingNothingChangesNothing()
    {
        GuideDirectoryGrain roster = NewRoster();
        await OnDutyAsync(roster, GuideA);
        await OnDutyAsync(roster, GuideB);

        GuideRequestOutcome offered = await roster.CreateRequestAsync(
            Requester,
            TourRequest,
            "hello",
            CancellationToken.None
        );

        int idle = offered.OfferedGuideId == GuideA ? GuideB : GuideA;

        // A late answer to a request that has since moved on must not disturb whoever holds it now.
        GuideRequestOutcome stray = await roster.GuideDecidesAsync(
            idle,
            true,
            CancellationToken.None
        );

        stray.Session.Should().BeNull();
        stray.Failed.Should().BeFalse();
        (await roster.GetSessionAsync(Requester, CancellationToken.None)).Should().BeNull();
    }
}
