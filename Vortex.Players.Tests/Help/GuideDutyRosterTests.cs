using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Vortex.Players.Grains;
using Vortex.Primitives.Help;
using Vortex.Tests.Support;
using Xunit;

namespace Vortex.Players.Tests.Help;

/// <summary>
/// The guide roster answers one question per queue: is anybody watching it. The counts therefore
/// overlap — one person covering all three is counted three times — and going off duty has to clear
/// every role, because the client keeps sending its three checkboxes even as it turns duty off.
/// </summary>
public sealed class GuideDutyRosterTests
{
    private static GuideDirectoryGrain NewRoster() =>
        GrainActivationContext.CreateWithIntegerKey<GuideDirectoryGrain>(0);

    [Fact]
    public async Task OneGuideCoveringEveryQueueIsCountedInEveryQueue()
    {
        GuideDirectoryGrain roster = NewRoster();

        GuideDutySnapshot duty = await roster.SetDutyAsync(
            playerId: 1,
            onDuty: true,
            handlesGuideRequests: true,
            handlesHelperRequests: true,
            handlesGuardianRequests: true,
            CancellationToken.None
        );

        duty.OnDuty.Should().BeTrue();
        duty.GuidesOnDuty.Should().Be(1);
        duty.HelpersOnDuty.Should().Be(1);
        duty.GuardiansOnDuty.Should().Be(1);
    }

    [Fact]
    public async Task RolesAreCountedSeparatelyAcrossGuides()
    {
        GuideDirectoryGrain roster = NewRoster();

        await roster.SetDutyAsync(1, true, true, false, false, CancellationToken.None);
        await roster.SetDutyAsync(2, true, true, true, false, CancellationToken.None);

        GuideDutySnapshot duty = await roster.SetDutyAsync(
            3,
            true,
            false,
            false,
            true,
            CancellationToken.None
        );

        duty.GuidesOnDuty.Should().Be(2);
        duty.HelpersOnDuty.Should().Be(1);
        duty.GuardiansOnDuty.Should().Be(1);
    }

    [Fact]
    public async Task GoingOffDutyClearsEveryRoleEvenWithTheBoxesStillTicked()
    {
        GuideDirectoryGrain roster = NewRoster();

        await roster.SetDutyAsync(1, true, true, true, true, CancellationToken.None);

        // The client sends the checkboxes unchanged on the change that takes them off duty; keeping
        // them would leave a guide counted as covering a queue they have just stepped away from.
        GuideDutySnapshot duty = await roster.SetDutyAsync(
            1,
            onDuty: false,
            handlesGuideRequests: true,
            handlesHelperRequests: true,
            handlesGuardianRequests: true,
            CancellationToken.None
        );

        duty.OnDuty.Should().BeFalse();
        duty.GuidesOnDuty.Should().Be(0);
        duty.HelpersOnDuty.Should().Be(0);
        duty.GuardiansOnDuty.Should().Be(0);
    }

    [Fact]
    public async Task ChangingRolesReplacesRatherThanAccumulates()
    {
        GuideDirectoryGrain roster = NewRoster();

        await roster.SetDutyAsync(1, true, true, true, true, CancellationToken.None);

        GuideDutySnapshot duty = await roster.SetDutyAsync(
            1,
            true,
            handlesGuideRequests: false,
            handlesHelperRequests: false,
            handlesGuardianRequests: true,
            CancellationToken.None
        );

        duty.GuidesOnDuty.Should().Be(0);
        duty.HelpersOnDuty.Should().Be(0);
        duty.GuardiansOnDuty.Should().Be(1);
    }

    [Fact]
    public async Task ADisconnectingGuideLeavesTheRoster()
    {
        GuideDirectoryGrain roster = NewRoster();

        await roster.SetDutyAsync(1, true, true, true, true, CancellationToken.None);
        await roster.ClearDutyAsync(1, CancellationToken.None);

        GuideDutySnapshot duty = await roster.GetStatusAsync(1, CancellationToken.None);

        duty.OnDuty.Should().BeFalse();
        duty.GuidesOnDuty.Should().Be(0);
    }

    [Fact]
    public async Task GetStatusReportsTheRosterWithoutJoiningIt()
    {
        GuideDirectoryGrain roster = NewRoster();

        await roster.SetDutyAsync(1, true, true, false, false, CancellationToken.None);

        GuideDutySnapshot duty = await roster.GetStatusAsync(2, CancellationToken.None);

        duty.OnDuty.Should().BeFalse();
        duty.GuidesOnDuty.Should().Be(1);
    }

    [Fact]
    public async Task AnUnboundSessionNeverEntersTheRoster()
    {
        GuideDirectoryGrain roster = NewRoster();

        GuideDutySnapshot duty = await roster.SetDutyAsync(
            0,
            true,
            true,
            true,
            true,
            CancellationToken.None
        );

        duty.OnDuty.Should().BeFalse();
        duty.GuidesOnDuty.Should().Be(0);
    }
}
