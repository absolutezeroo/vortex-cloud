using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Context;
using Vortex.Database.Entities.Moderation;
using Vortex.Primitives.Moderation;
using Xunit;

namespace Vortex.Rooms.Tests.Moderation;

/// <summary>
/// A player's own view of their open reports. The client asks for this list before it will let them
/// file another and offers to withdraw what is there, so two rules carry real weight: the list must
/// be theirs alone, and withdrawing must not reach a report a moderator has already picked up.
/// </summary>
public sealed class CfhPendingCallsTests
{
    private const int REPORTER = 10;
    private const int OTHER_REPORTER = 11;

    [Fact]
    public async Task GetPendingForReporter_ReturnsOnlyTheirOwnOpenReportsNewestFirst()
    {
        DbContextOptions<VortexDbContext> options = NewOptions();

        await using (VortexDbContext db = new(options))
        {
            db.CfhTickets.AddRange(
                NewTicket(1, REPORTER, CfhTicketState.Open, "oldest", DaysAgo(3)),
                NewTicket(2, REPORTER, CfhTicketState.Picked, "picked up", DaysAgo(1)),
                NewTicket(3, REPORTER, CfhTicketState.Closed, "already dealt with", DaysAgo(2)),
                NewTicket(4, OTHER_REPORTER, CfhTicketState.Open, "somebody else", DaysAgo(1))
            );

            await db.SaveChangesAsync();
        }

        ImmutableArray<CfhPendingCallSnapshot> calls = await NewService(options)
            .GetPendingForReporterAsync(REPORTER);

        // Picked counts as pending -- it is still open as far as the reporter is concerned -- while
        // closed does not, and another player's report never appears.
        calls.Select(c => c.Message).Should().Equal("picked up", "oldest");
        calls.Select(c => c.CallId).Should().Equal("2", "1");
    }

    [Fact]
    public async Task GetPendingForReporter_IgnoresSoftDeletedReports()
    {
        DbContextOptions<VortexDbContext> options = NewOptions();

        await using (VortexDbContext db = new(options))
        {
            CfhTicketEntity withdrawn = NewTicket(
                1,
                REPORTER,
                CfhTicketState.Open,
                "withdrawn",
                DaysAgo(1)
            );
            withdrawn.DeletedAt = DateTime.UtcNow;

            db.CfhTickets.Add(withdrawn);
            await db.SaveChangesAsync();
        }

        (await NewService(options).GetPendingForReporterAsync(REPORTER)).Should().BeEmpty();
    }

    [Fact]
    public async Task DeletePendingForReporter_LeavesAReportAModeratorHasPickedUp()
    {
        DbContextOptions<VortexDbContext> options = NewOptions();

        await using (VortexDbContext db = new(options))
        {
            db.CfhTickets.AddRange(
                NewTicket(1, REPORTER, CfhTicketState.Open, "withdrawable", DaysAgo(1)),
                NewTicket(2, REPORTER, CfhTicketState.Picked, "in staff hands", DaysAgo(1)),
                NewTicket(3, OTHER_REPORTER, CfhTicketState.Open, "not theirs", DaysAgo(1))
            );

            await db.SaveChangesAsync();
        }

        int withdrawn = await NewService(options).DeletePendingForReporterAsync(REPORTER);

        withdrawn.Should().Be(1);

        await using VortexDbContext check = new(options);

        // The picked one survives: a moderator is already looking at it, and letting the reporter
        // pull it out from under them would erase work in progress.
        check.CfhTickets.IgnoreQueryFilters().Single(t => t.Id == 2).DeletedAt.Should().BeNull();
        check.CfhTickets.IgnoreQueryFilters().Single(t => t.Id == 3).DeletedAt.Should().BeNull();
        check.CfhTickets.IgnoreQueryFilters().Single(t => t.Id == 1).DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task DeletePendingForReporter_WithdrawsRatherThanCloses()
    {
        DbContextOptions<VortexDbContext> options = NewOptions();

        await using (VortexDbContext db = new(options))
        {
            db.CfhTickets.Add(NewTicket(1, REPORTER, CfhTicketState.Open, "mine", DaysAgo(1)));
            await db.SaveChangesAsync();
        }

        await NewService(options).DeletePendingForReporterAsync(REPORTER);

        await using VortexDbContext check = new(options);

        // Soft-deleted, not Closed: a withdrawal is not a moderation outcome, and counting it as one
        // would inflate every "tickets handled" figure the dashboard draws.
        CfhTicketEntity ticket = check.CfhTickets.IgnoreQueryFilters().Single(t => t.Id == 1);

        ticket.State.Should().Be(CfhTicketState.Open);
        ticket.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPendingForReporter_RefusesAnUnboundSession()
    {
        (await NewService(NewOptions()).GetPendingForReporterAsync(0)).Should().BeEmpty();
        (await NewService(NewOptions()).DeletePendingForReporterAsync(-1)).Should().Be(0);
    }

    private static DateTime DaysAgo(int days) => DateTime.UtcNow.AddDays(-days);

    private static DbContextOptions<VortexDbContext> NewOptions() =>
        new DbContextOptionsBuilder<VortexDbContext>()
            .UseInMemoryDatabase($"cfh-pending-{Guid.NewGuid():N}")
            .Options;

    private static ICfhTicketService NewService(DbContextOptions<VortexDbContext> options) =>
        new CfhTicketService(new SingleOptionsFactory(options));

    private static CfhTicketEntity NewTicket(
        int id,
        int reporterId,
        CfhTicketState state,
        string message,
        DateTime createdAt
    ) =>
        new()
        {
            Id = id,
            State = state,
            ReporterPlayerEntityId = reporterId,
            ReportedPlayerEntityId = 99,
            CfhTopicEntityId = 1,
            Message = message,
            CreatedAt = createdAt,
        };

    private sealed class SingleOptionsFactory(DbContextOptions<VortexDbContext> options)
        : IDbContextFactory<VortexDbContext>
    {
        public VortexDbContext CreateDbContext() => new(options);
    }
}
