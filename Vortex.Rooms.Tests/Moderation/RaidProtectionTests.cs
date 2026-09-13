using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Context;
using Vortex.Database.Entities.Room;
using Vortex.Primitives.Events;
using Vortex.Primitives.Orleans.Snapshots.Room;
using Vortex.Primitives.Permissions;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.RaidProtection;
using Vortex.Rooms.Tests.Support;
using Xunit;

namespace Vortex.Rooms.Tests.Moderation;

/// <summary>
/// The room's raid detector.
/// </summary>
/// <remarks>
/// Worth saying once, here, where the numbers are: the settings panel is Habbo's and the thresholds
/// are not. No client build carries one, so these tests pin down <em>our</em> rule — a sliding
/// window of distinct arrivals — and the two properties that make it safe to run at all: the people
/// who can turn it off are never caught by it, and one account cannot trip it on its own.
/// </remarks>
public sealed class RaidProtectionTests
{
    private static readonly PlayerId[] Raiders =
    [
        new(500),
        new(501),
        new(502),
        new(503),
        new(504),
        new(505),
        new(506),
    ];

    [Fact]
    public async Task WithProtectionOffACrowdWalksStraightIn()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);

        foreach (PlayerId raider in Raiders)
        {
            RaidEntryDecision decision = await harness
                .Grain.EvaluateEntryAsync(raider, CancellationToken.None)
                .ConfigureAwait(true);

            decision.Verdict.Should().Be(RaidEntryVerdict.Allow);
        }
    }

    [Fact]
    public async Task OnHighTheSixthArrivalInAMinuteIsTurnedAway()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        await EnableAsync(harness, RaidDetectionSensitivity.High).ConfigureAwait(true);

        // High is five. The fifth arrival is the last one under the threshold; it is only the sixth
        // that crosses it, which is what keeps a room of exactly five visitors usable.
        foreach (PlayerId raider in Raiders.Take(5))
        {
            RaidEntryDecision allowed = await harness
                .Grain.EvaluateEntryAsync(raider, CancellationToken.None)
                .ConfigureAwait(true);

            allowed.Verdict.Should().Be(RaidEntryVerdict.Allow);
        }

        RaidEntryDecision sixth = await harness
            .Grain.EvaluateEntryAsync(Raiders[5], CancellationToken.None)
            .ConfigureAwait(true);

        sixth.Verdict.Should().Be(RaidEntryVerdict.Kick);
        harness.Grain._state.RaidProtection.IncidentActive.Should().BeTrue();

        harness
            .PublishedEvents.OfType<RoomRaidDetectedEvent>()
            .Should()
            .ContainSingle()
            .Which.Threshold.Should()
            .Be(5);
    }

    [Fact]
    public async Task OneAccountReconnectingForeverCannotTripTheRoomOnItsOwn()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        await EnableAsync(harness, RaidDetectionSensitivity.High).ConfigureAwait(true);

        for (int attempt = 0; attempt < 20; attempt++)
        {
            RaidEntryDecision decision = await harness
                .Grain.EvaluateEntryAsync(Raiders[0], CancellationToken.None)
                .ConfigureAwait(true);

            decision.Verdict.Should().Be(RaidEntryVerdict.Allow);
        }

        harness.Grain._state.RaidProtection.IncidentActive.Should().BeFalse();
    }

    [Fact]
    public async Task TheOwnerWalksThroughTheirOwnRaid()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        await EnableAsync(harness, RaidDetectionSensitivity.High).ConfigureAwait(true);

        foreach (PlayerId raider in Raiders)
        {
            await harness
                .Grain.EvaluateEntryAsync(raider, CancellationToken.None)
                .ConfigureAwait(true);
        }

        harness.Grain._state.RaidProtection.IncidentActive.Should().BeTrue();

        // The whole point: the switch that stops this is behind the owner's own front door.
        RaidEntryDecision owner = await harness
            .Grain.EvaluateEntryAsync(RoomHarness.Owner, CancellationToken.None)
            .ConfigureAwait(true);

        owner.Verdict.Should().Be(RaidEntryVerdict.Allow);
        owner.CanManage.Should().BeTrue();
    }

    [Fact]
    public async Task StaffAreNotCaughtInIt()
    {
        RoomHarness harness = await RoomHarness
            .CreateAsync(capabilities: [Capabilities.Room.ModerateAny])
            .ConfigureAwait(true);
        await EnableAsync(harness, RaidDetectionSensitivity.High).ConfigureAwait(true);

        foreach (PlayerId raider in Raiders)
        {
            RaidEntryDecision decision = await harness
                .Grain.EvaluateEntryAsync(raider, CancellationToken.None)
                .ConfigureAwait(true);

            decision.Verdict.Should().Be(RaidEntryVerdict.Allow);
        }
    }

    [Fact]
    public async Task OnTemporaryBanTheArrivalIsWrittenIntoTheRoomsBanList()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        await EnableAsync(
                harness,
                RaidDetectionSensitivity.High,
                action: RaidProtectionAction.TemporaryBan
            )
            .ConfigureAwait(true);

        foreach (PlayerId raider in Raiders.Take(5))
        {
            await harness
                .Grain.EvaluateEntryAsync(raider, CancellationToken.None)
                .ConfigureAwait(true);
        }

        RaidEntryDecision banned = await harness
            .Grain.EvaluateEntryAsync(Raiders[5], CancellationToken.None)
            .ConfigureAwait(true);

        banned.Verdict.Should().Be(RaidEntryVerdict.Ban);
        banned.BanDurationSeconds.Should().Be(3600);

        await using VortexDbContext db = harness.NewDbContext();

        RoomBanEntity row = await db
            .RoomBans.SingleAsync(b => b.PlayerEntityId == Raiders[5].Value)
            .ConfigureAwait(true);

        row.DateExpires.Should().BeCloseTo(DateTime.UtcNow.AddHours(1), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task WhenTheRoomQuietensTheIncidentClosesAndTheGuardTakesOver()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        await EnableAsync(harness, RaidDetectionSensitivity.High, guard: true).ConfigureAwait(true);

        foreach (PlayerId raider in Raiders)
        {
            await harness
                .Grain.EvaluateEntryAsync(raider, CancellationToken.None)
                .ConfigureAwait(true);
        }

        harness.Grain._state.RaidProtection.IncidentActive.Should().BeTrue();

        // A tick a minute and a half later: the window has emptied, so the incident is over and the
        // guard period it leaves behind is what watches for the second wave.
        await harness
            .Grain.RaidProtectionSystem.TickAsync(
                DateTime.UtcNow.AddSeconds(90),
                CancellationToken.None
            )
            .ConfigureAwait(true);

        harness.Grain._state.RaidProtection.IncidentActive.Should().BeFalse();
        harness.Grain._state.RaidProtection.GuardUntilUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task ADurationTheOfficialPanelCannotProduceIsRefused()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);

        // 30 years. No dropdown offers it; a modified client asking for it is the reason the server
        // checks the value lists at all.
        RaidProtectionSaveOutcome outcome = await harness
            .Grain.SaveRaidProtectionAsync(
                harness.ContextFor(RoomHarness.Owner),
                Draft() with
                {
                    BanDurationSeconds = 946_080_000,
                },
                confirmed: false,
                CancellationToken.None
            )
            .ConfigureAwait(true);

        outcome.Result.Should().Be(RaidProtectionSaveResult.InvalidSettings);

        // And the reply still carries the room's real state: the client repaints its whole panel
        // from these, so echoing the refused draft back would show settings the room does not hold.
        outcome.Settings.Enabled.Should().BeFalse();
        outcome.Settings.BanDurationSeconds.Should().Be(3600);
        outcome.Settings.RoomId.Should().Be(RoomHarness.RoomIdValue);
    }

    [Fact]
    public async Task AVisitorWithNoRightsCannotConfigureSomebodyElsesRoom()
    {
        // canManipulate:false — otherwise the harness hands Stranger rights, and rights are exactly
        // what this feature grants access on.
        RoomHarness harness = await RoomHarness
            .CreateAsync(canManipulate: false)
            .ConfigureAwait(true);

        RaidProtectionSaveOutcome outcome = await harness
            .Grain.SaveRaidProtectionAsync(
                harness.ContextFor(RoomHarness.Stranger),
                Draft() with
                {
                    Enabled = true,
                },
                confirmed: false,
                CancellationToken.None
            )
            .ConfigureAwait(true);

        outcome.Result.Should().Be(RaidProtectionSaveResult.NotAllowed);
        harness.Grain._state.RaidProtection.Enabled.Should().BeFalse();

        RoomRaidProtectionSnapshot? seen = await harness
            .Grain.GetRaidProtectionAsync(RoomHarness.Stranger)
            .ConfigureAwait(true);

        seen.Should().BeNull();
    }

    /// <summary>
    /// Sulake's rule, not an inference: the announcement says the menu is reachable by the owner
    /// "and Habbos with rights to the room". This started out owner-only and was wrong.
    /// </summary>
    [Fact]
    public async Task ARightsHolderGetsThePanelToo()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);

        RaidProtectionSaveOutcome outcome = await harness
            .Grain.SaveRaidProtectionAsync(
                harness.ContextFor(RoomHarness.Stranger),
                Draft() with
                {
                    Enabled = true,
                },
                confirmed: false,
                CancellationToken.None
            )
            .ConfigureAwait(true);

        outcome.Result.Should().Be(RaidProtectionSaveResult.Ok);

        RoomRaidProtectionSnapshot? seen = await harness
            .Grain.GetRaidProtectionAsync(RoomHarness.Stranger)
            .ConfigureAwait(true);

        seen.Should().NotBeNull();
        seen!.Enabled.Should().BeTrue();

        // And the capability that unlocks it client-side says so on their way in.
        RaidEntryDecision entry = await harness
            .Grain.EvaluateEntryAsync(RoomHarness.Stranger, CancellationToken.None)
            .ConfigureAwait(true);

        entry.CanManage.Should().BeTrue();
    }

    [Fact]
    public async Task SettingsSurviveTheRoomUnloading()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        await EnableAsync(harness, RaidDetectionSensitivity.Low).ConfigureAwait(true);

        await using VortexDbContext db = harness.NewDbContext();

        RoomRaidProtectionEntity row = await db
            .RoomRaidProtection.SingleAsync(r => r.RoomEntityId == RoomHarness.RoomIdValue)
            .ConfigureAwait(true);

        row.Enabled.Should().BeTrue();
        row.DetectionSensitivity.Should().Be((int)RaidDetectionSensitivity.Low);
    }

    private static async Task EnableAsync(
        RoomHarness harness,
        RaidDetectionSensitivity sensitivity,
        RaidProtectionAction action = RaidProtectionAction.Kick,
        bool guard = false
    )
    {
        RaidProtectionSaveOutcome outcome = await harness
            .Grain.SaveRaidProtectionAsync(
                harness.ContextFor(RoomHarness.Owner),
                Draft() with
                {
                    Enabled = true,
                    DetectionSensitivity = (int)sensitivity,
                    ActionType = (int)action,
                    GuardEnabled = guard,
                },
                confirmed: false,
                CancellationToken.None
            )
            .ConfigureAwait(true);

        outcome.Result.Should().Be(RaidProtectionSaveResult.Ok);
    }

    private static RoomRaidProtectionSnapshot Draft() =>
        new()
        {
            RoomId = RoomHarness.RoomIdValue,
            Enabled = false,
            DetectionSensitivity = (int)RaidDetectionSensitivity.Medium,
            ActionType = (int)RaidProtectionAction.Kick,
            BanDurationSeconds = 3600,
            GuardEnabled = false,
            GuardDurationSeconds = 900,
            GuardSensitivity = (int)RaidDetectionSensitivity.High,
            IncidentActive = false,
            LastRaidAtEpochSeconds = 0,
        };
}
