using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Context;
using Vortex.Database.Entities.Moderation;
using Vortex.Database.Entities.Players;
using Vortex.Primitives.Events;
using Vortex.Primitives.Moderation;
using Vortex.Primitives.Players.Enums;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Tests.Support;
using Xunit;

namespace Vortex.Rooms.Tests.Moderation;

/// <summary>
/// Claiming a CFH ticket. The mod tool has an auto-pick button, so two moderators reaching for the
/// same ticket is the normal case rather than the exotic one — and the client has a whole rejection
/// and retry path that only works if the loser is actually told they lost, and by whom.
/// </summary>
public sealed class CfhTicketPickContentionTests
{
    private const int FIRST_MODERATOR = 20;
    private const int SECOND_MODERATOR = 21;

    [Fact]
    public async Task Pick_TakesAnOpenTicketAndNamesTheCallerAsItsHolder()
    {
        DbContextOptions<VortexDbContext> options = await SeedAsync(
            NewTicket(1, CfhTicketState.Open, picker: null)
        );

        ImmutableArray<CfhTicketPickOutcome> outcomes = await NewService(options)
            .PickTicketsAsync([1], FIRST_MODERATOR);

        outcomes.Should().ContainSingle();
        outcomes[0].Acquired.Should().BeTrue();
        outcomes[0].PickerPlayerId.Should().Be(FIRST_MODERATOR);
        outcomes[0].PickerPlayerName.Should().Be("ModOne");

        await using VortexDbContext check = new(options);
        CfhTicketEntity stored = await check.CfhTickets.SingleAsync(t => t.Id == 1);

        stored.State.Should().Be(CfhTicketState.Picked);
        stored.PickerPlayerEntityId.Should().Be(FIRST_MODERATOR);
    }

    [Fact]
    public async Task Pick_RefusesATicketSomebodyElseAlreadyHolds_AndNamesThem()
    {
        DbContextOptions<VortexDbContext> options = await SeedAsync(
            NewTicket(1, CfhTicketState.Picked, picker: FIRST_MODERATOR)
        );

        ImmutableArray<CfhTicketPickOutcome> outcomes = await NewService(options)
            .PickTicketsAsync([1], SECOND_MODERATOR);

        outcomes.Should().ContainSingle();
        outcomes[0].Acquired.Should().BeFalse();

        // The name is the point: the client's rejection dialog exists to say who got there first.
        outcomes[0].PickerPlayerId.Should().Be(FIRST_MODERATOR);
        outcomes[0].PickerPlayerName.Should().Be("ModOne");

        await using VortexDbContext check = new(options);

        // And crucially the loser must not have overwritten the winner.
        (await check.CfhTickets.SingleAsync(t => t.Id == 1))
            .PickerPlayerEntityId.Should()
            .Be(FIRST_MODERATOR);
    }

    [Fact]
    public async Task Pick_SecondCallerLosesTheContestedOneButStillTakesTheFreeOne()
    {
        DbContextOptions<VortexDbContext> options = await SeedAsync(
            NewTicket(1, CfhTicketState.Open, picker: null),
            NewTicket(2, CfhTicketState.Open, picker: null)
        );

        ICfhTicketService service = NewService(options);

        await service.PickTicketsAsync([1], FIRST_MODERATOR);

        // The bundle the second moderator's tool drew still lists both.
        ImmutableArray<CfhTicketPickOutcome> outcomes = await service.PickTicketsAsync(
            [1, 2],
            SECOND_MODERATOR
        );

        // Partial application, reported per id — not an all-or-nothing failure that would cost them
        // the ticket nobody was competing for.
        outcomes.Should().HaveCount(2);
        outcomes.Single(o => o.IssueId == 1).Acquired.Should().BeFalse();
        outcomes.Single(o => o.IssueId == 2).Acquired.Should().BeTrue();
    }

    [Fact]
    public async Task Pick_ReportsAVerdictForAnIdThatDoesNotExist()
    {
        DbContextOptions<VortexDbContext> options = await SeedAsync();

        ImmutableArray<CfhTicketPickOutcome> outcomes = await NewService(options)
            .PickTicketsAsync([404], FIRST_MODERATOR);

        // Silently dropping it would leave the client's list showing a ticket forever.
        outcomes.Should().ContainSingle();
        outcomes[0].Acquired.Should().BeFalse();
        outcomes[0].PickerPlayerId.Should().Be(0);
    }

    [Fact]
    public async Task Pick_RefusesAnAlreadyClosedTicket()
    {
        DbContextOptions<VortexDbContext> options = await SeedAsync(
            NewTicket(1, CfhTicketState.Closed, picker: FIRST_MODERATOR)
        );

        ImmutableArray<CfhTicketPickOutcome> outcomes = await NewService(options)
            .PickTicketsAsync([1], SECOND_MODERATOR);

        outcomes[0].Acquired.Should().BeFalse();

        await using VortexDbContext check = new(options);
        (await check.CfhTickets.SingleAsync(t => t.Id == 1))
            .State.Should()
            .Be(CfhTicketState.Closed);
    }

    [Fact]
    public async Task Release_ReportsOnlyWhatItActuallyHandedBack()
    {
        DbContextOptions<VortexDbContext> options = await SeedAsync(
            NewTicket(1, CfhTicketState.Picked, picker: FIRST_MODERATOR),
            NewTicket(2, CfhTicketState.Open, picker: null)
        );

        ImmutableArray<int> released = await NewService(options).ReleaseTicketsAsync([1, 2]);

        // 2 was never picked, so releasing it is not a state change and must not be republished.
        released.Should().Equal(1);
    }

    private static async Task<DbContextOptions<VortexDbContext>> SeedAsync(
        params CfhTicketEntity[] tickets
    )
    {
        DbContextOptions<VortexDbContext> options = new DbContextOptionsBuilder<VortexDbContext>()
            .UseInMemoryDatabase($"cfh-pick-{Guid.NewGuid():N}")
            .Options;

        await using VortexDbContext db = new(options);

        await db.Players.AddRangeAsync(
            NewPlayer(FIRST_MODERATOR, "ModOne"),
            NewPlayer(SECOND_MODERATOR, "ModTwo")
        );

        if (tickets.Length > 0)
        {
            await db.CfhTickets.AddRangeAsync(tickets);
        }

        await db.SaveChangesAsync();

        return options;
    }

    private static ICfhTicketService NewService(DbContextOptions<VortexDbContext> options) =>
        new CfhTicketService(
            new SingleOptionsFactory(options),
            FakeProxy.Create<IEventPublisher>(_ => Task.CompletedTask)
        );

    private static PlayerEntity NewPlayer(int id, string name) =>
        new()
        {
            Id = id,
            Name = name,
            Figure = string.Empty,
            Gender = AvatarGenderType.Male,
            PlayerStatus = PlayerStatusType.Online,
            PlayerPerks = PlayerPerkFlags.None,
        };

    private static CfhTicketEntity NewTicket(int id, CfhTicketState state, int? picker) =>
        new()
        {
            Id = id,
            State = state,
            ReporterPlayerEntityId = 99,
            ReportedPlayerEntityId = 98,
            CfhTopicEntityId = 1,
            Message = "help",
            PickerPlayerEntityId = picker,
            CreatedAt = DateTime.UtcNow,
        };

    private sealed class SingleOptionsFactory(DbContextOptions<VortexDbContext> options)
        : IDbContextFactory<VortexDbContext>
    {
        public VortexDbContext CreateDbContext() => new(options);
    }
}
