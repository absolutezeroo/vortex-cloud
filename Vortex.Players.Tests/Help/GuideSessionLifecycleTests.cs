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
/// Living and ending a guide session. The chat and the typing indicator carry no destination, so the
/// session is the only thing that says where a line may go — and closing has to reach both the
/// paired case and the one where the requester walks away before any guide accepted.
/// </summary>
public sealed class GuideSessionLifecycleTests
{
    private const int Requester = 100;
    private const int GuideA = 1;
    private const int GuideB = 2;

    private static GuideDirectoryGrain NewRoster() => GuideRosterHarness.New();

    private static async Task<GuideDirectoryGrain> PairedAsync()
    {
        GuideDirectoryGrain roster = NewRoster();

        await roster.SetDutyAsync(GuideA, true, true, true, false, CancellationToken.None);

        GuideRequestOutcome offered = await roster.CreateRequestAsync(
            Requester,
            0,
            "hello",
            CancellationToken.None
        );
        await roster.GuideDecidesAsync(offered.OfferedGuideId, true, CancellationToken.None);

        return roster;
    }

    [Fact]
    public async Task EachSideOfAPairSeesTheOther()
    {
        GuideDirectoryGrain roster = await PairedAsync();

        (await roster.GetPartnerAsync(Requester, CancellationToken.None)).Should().Be(GuideA);
        (await roster.GetPartnerAsync(GuideA, CancellationToken.None)).Should().Be(Requester);
    }

    [Fact]
    public async Task SomebodyInNoSessionHasNoPartner()
    {
        GuideDirectoryGrain roster = await PairedAsync();

        // What stops a stray chat line going to whoever happened to be paired last.
        (await roster.GetPartnerAsync(999, CancellationToken.None))
            .Should()
            .Be(0);
    }

    [Fact]
    public async Task EndingASessionReturnsThePartnerAndClearsBothSides()
    {
        GuideDirectoryGrain roster = await PairedAsync();

        int partner = await roster.EndSessionAsync(GuideA, CancellationToken.None);

        partner.Should().Be(Requester);
        (await roster.GetSessionAsync(Requester, CancellationToken.None)).Should().BeNull();
        (await roster.GetSessionAsync(GuideA, CancellationToken.None)).Should().BeNull();
        (await roster.GetPartnerAsync(Requester, CancellationToken.None)).Should().Be(0);
    }

    [Fact]
    public async Task EndingTwiceReachesNobodyTheSecondTime()
    {
        GuideDirectoryGrain roster = await PairedAsync();

        await roster.EndSessionAsync(GuideA, CancellationToken.None);

        // The caller sends nothing on a zero, so a double close cannot take the requester out of the
        // feedback form it just put them in.
        (await roster.EndSessionAsync(GuideA, CancellationToken.None))
            .Should()
            .Be(0);
    }

    [Fact]
    public async Task ARequesterWhoWalksAwayFreesTheGuideStillHoldingTheirOffer()
    {
        GuideDirectoryGrain roster = NewRoster();

        await roster.SetDutyAsync(GuideA, true, true, true, false, CancellationToken.None);

        await roster.CreateRequestAsync(Requester, 0, "hello", CancellationToken.None);
        await roster.EndSessionAsync(Requester, CancellationToken.None);

        // The offer must not still be sitting in front of GuideA: another player asking now has to
        // find them free.
        GuideRequestOutcome next = await roster.CreateRequestAsync(
            101,
            0,
            "anyone?",
            CancellationToken.None
        );

        next.Failed.Should().BeFalse();
        next.OfferedGuideId.Should().Be(GuideA);
    }

    [Fact]
    public async Task EndingAFinishedSessionLetsTheGuideTakeAnother()
    {
        GuideDirectoryGrain roster = await PairedAsync();

        await roster.EndSessionAsync(GuideA, CancellationToken.None);

        GuideRequestOutcome next = await roster.CreateRequestAsync(
            101,
            0,
            "next please",
            CancellationToken.None
        );

        next.OfferedGuideId.Should().Be(GuideA);
    }

    [Fact]
    public async Task ACancelledRequesterCanAskAgain()
    {
        GuideDirectoryGrain roster = NewRoster();

        await roster.SetDutyAsync(GuideA, true, true, true, false, CancellationToken.None);
        await roster.SetDutyAsync(GuideB, true, true, true, false, CancellationToken.None);

        await roster.CreateRequestAsync(Requester, 0, "first", CancellationToken.None);
        await roster.EndSessionAsync(Requester, CancellationToken.None);

        // The one-request-per-player rule must not outlive the request it was guarding.
        GuideRequestOutcome again = await roster.CreateRequestAsync(
            Requester,
            0,
            "second",
            CancellationToken.None
        );

        again.Failed.Should().BeFalse();
    }
}
