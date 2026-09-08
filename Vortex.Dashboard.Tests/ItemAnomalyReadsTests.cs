using System;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Vortex.Dashboard.API.Api.Safety;
using Vortex.Dashboard.API.Api.Safety.Contracts;
using Vortex.Database.Context;
using Vortex.Database.Entities.Audit;
using Vortex.Primitives.Observability;
using Xunit;

namespace Vortex.Dashboard.Tests;

/// <summary>
/// The three duplication signatures read off the item journal.
/// </summary>
/// <remarks>
/// Worth testing rather than eyeballing: two of the three depend on the ORDER events happened in,
/// which no compiler checks and which a wrong answer hides behind a plausible-looking number. A
/// scan that quietly finds nothing is indistinguishable from a hotel with nothing wrong, and that
/// is the failure mode this file exists to prevent.
/// </remarks>
public sealed class ItemAnomalyReadsTests
{
    private static readonly DateTime Now = new(2026, 9, 8, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task An_item_created_twice_is_flagged()
    {
        DbContextOptions<VortexDbContext> options = NewOptions();
        await SeedAsync(
            options,
            Event(1, ItemEventType.Created, Now.AddHours(-5)),
            Event(1, ItemEventType.Created, Now.AddHours(-4))
        );

        ItemAnomaly anomaly = (await ScanAsync(options)).Items.Should().ContainSingle().Subject;

        anomaly.ItemId.Should().Be(1);
        anomaly.Kind.Should().Be("born_twice");
        anomaly.Occurrences.Should().Be(2);
    }

    [Fact]
    public async Task An_item_created_once_is_not()
    {
        DbContextOptions<VortexDbContext> options = NewOptions();
        await SeedAsync(
            options,
            Event(1, ItemEventType.Created, Now.AddHours(-5)),
            Event(1, ItemEventType.Placed, Now.AddHours(-4), roomId: 10),
            Event(1, ItemEventType.PickedUp, Now.AddHours(-3), roomId: 10)
        );

        (await ScanAsync(options)).Items.Should().BeEmpty("an ordinary life is not an anomaly");
    }

    [Fact]
    public async Task An_item_that_keeps_living_after_deletion_is_flagged()
    {
        DbContextOptions<VortexDbContext> options = NewOptions();
        await SeedAsync(
            options,
            Event(2, ItemEventType.Deleted, Now.AddHours(-5)),
            Event(2, ItemEventType.Traded, Now.AddHours(-4))
        );

        ItemAnomaly anomaly = (await ScanAsync(options)).Items.Should().ContainSingle().Subject;

        anomaly.Kind.Should().Be("resurrected");
        anomaly.ItemId.Should().Be(2);
    }

    [Fact]
    public async Task Events_before_the_deletion_are_not_a_resurrection()
    {
        DbContextOptions<VortexDbContext> options = NewOptions();
        await SeedAsync(
            options,
            Event(2, ItemEventType.Traded, Now.AddHours(-6)),
            Event(2, ItemEventType.Deleted, Now.AddHours(-5))
        );

        (await ScanAsync(options))
            .Items.Should()
            .BeEmpty("living and then being deleted is the ordinary order");
    }

    [Fact]
    public async Task An_item_placed_in_a_second_room_without_a_pickup_is_flagged()
    {
        DbContextOptions<VortexDbContext> options = NewOptions();
        await SeedAsync(
            options,
            Event(3, ItemEventType.Placed, Now.AddHours(-5), roomId: 10),
            Event(3, ItemEventType.Placed, Now.AddHours(-4), roomId: 20)
        );

        ItemAnomaly anomaly = (await ScanAsync(options)).Items.Should().ContainSingle().Subject;

        anomaly.Kind.Should().Be("two_places");
        anomaly.Occurrences.Should().Be(1);
    }

    [Fact]
    public async Task Moving_a_item_room_to_room_properly_is_not_flagged()
    {
        // The whole point of walking the order: this is the same three rooms as a duplication, with
        // the pickups that make it legitimate. A signature that groups instead of walking calls it
        // an anomaly.
        DbContextOptions<VortexDbContext> options = NewOptions();
        await SeedAsync(
            options,
            Event(4, ItemEventType.Placed, Now.AddHours(-6), roomId: 10),
            Event(4, ItemEventType.PickedUp, Now.AddHours(-5), roomId: 10),
            Event(4, ItemEventType.Placed, Now.AddHours(-4), roomId: 20),
            Event(4, ItemEventType.PickedUp, Now.AddHours(-3), roomId: 20),
            Event(4, ItemEventType.Placed, Now.AddHours(-2), roomId: 30)
        );

        (await ScanAsync(options)).Items.Should().BeEmpty("each move was a pickup then a place");
    }

    [Fact]
    public async Task Anything_outside_the_window_is_not_scanned()
    {
        DbContextOptions<VortexDbContext> options = NewOptions();
        await SeedAsync(
            options,
            Event(5, ItemEventType.Created, Now.AddDays(-40)),
            Event(5, ItemEventType.Created, Now.AddDays(-39))
        );

        ItemAnomalyScan scan = await ScanAsync(options);

        scan.Items.Should().BeEmpty();
        // The count is what lets an empty result be read as "nothing wrong" rather than
        // "nothing looked at".
        scan.ItemsScanned.Should().Be(0);
    }

    [Fact]
    public async Task A_widened_window_reaches_back_to_it()
    {
        DbContextOptions<VortexDbContext> options = NewOptions();
        await SeedAsync(
            options,
            Event(5, ItemEventType.Created, Now.AddDays(-40)),
            Event(5, ItemEventType.Created, Now.AddDays(-39))
        );

        ItemAnomalyScan scan = await ScanAsync(options, since: Now.AddDays(-60));

        scan.Items.Should().ContainSingle().Which.Kind.Should().Be("born_twice");
        scan.ItemsScanned.Should().Be(1);
    }

    // ── Harness ──────────────────────────────────────────────────────────────

    private static DbContextOptions<VortexDbContext> NewOptions() =>
        new DbContextOptionsBuilder<VortexDbContext>()
            .UseInMemoryDatabase($"item-anomaly-{Guid.NewGuid():N}")
            .Options;

    private static ItemEventEntity Event(
        long itemId,
        ItemEventType type,
        DateTime at,
        int? roomId = null
    ) =>
        new()
        {
            ItemId = itemId,
            EventType = type,
            OccurredAt = at,
            RoomId = roomId,
        };

    private static async Task SeedAsync(
        DbContextOptions<VortexDbContext> options,
        params ItemEventEntity[] events
    )
    {
        await using VortexDbContext db = new(options);

        db.ItemEvents.AddRange(events);
        await db.SaveChangesAsync();
    }

    private static Task<ItemAnomalyScan> ScanAsync(
        DbContextOptions<VortexDbContext> options,
        DateTime? since = null
    )
    {
        NameValueCollection query = new() { ["until"] = Now.ToString("o") };

        if (since is { } from)
        {
            query["since"] = from.ToString("o");
        }

        return new ItemAnomalyReads(new TestContextFactory(options)).ScanAsync(
            query,
            CancellationToken.None
        );
    }

    private sealed class TestContextFactory(DbContextOptions<VortexDbContext> options)
        : IDbContextFactory<VortexDbContext>
    {
        public VortexDbContext CreateDbContext() => new(options);
    }
}
