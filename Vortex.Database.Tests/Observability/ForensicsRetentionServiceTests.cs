using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Vortex.Database.Context;
using Vortex.Database.Entities.Audit;
using Vortex.Database.Entities.Commerce;
using Vortex.Observability.Configuration;
using Vortex.Observability.Runtime;
using Vortex.Primitives.Commerce;
using Vortex.Primitives.Observability;
using Xunit;

namespace Vortex.Database.Tests.Observability;

/// <summary>
/// The sweep decides what is deleted from the tables the moderation tooling reads live, so the two
/// things worth pinning are the cutoff (nothing inside the window is ever touched) and the opt-out
/// (a retention of 0 means keep, not delete everything).
/// </summary>
public sealed class ForensicsRetentionServiceTests : IAsyncLifetime
{
    private SqliteConnection _conn = null!;
    private DbContextOptions<VortexDbContext> _options = null!;

    public async Task InitializeAsync()
    {
        _conn = new SqliteConnection("Filename=:memory:");
        await _conn.OpenAsync();
        _options = new DbContextOptionsBuilder<VortexDbContext>().UseSqlite(_conn).Options;

        await using VortexDbContext db = new(_options);
        await db.Database.EnsureCreatedAsync();
        await db.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = OFF");
    }

    public async Task DisposeAsync() => await _conn.DisposeAsync();

    [Fact]
    public async Task TheSweep_RemovesOnlyWhatIsOlderThanTheWindow()
    {
        await SeedAuditAsync(ageInDays: 200);
        await SeedAuditAsync(ageInDays: 10);

        await RunSweepAsync(new ObservabilityConfig { AuditRetentionDays = 180 });

        await using VortexDbContext db = new(_options);
        AuditEventEntity[] left = await db.AuditEvents.AsNoTracking().ToArrayAsync();

        left.Should().ContainSingle("the row inside the window is still evidence");
        (DateTime.UtcNow - left[0].OccurredAt).TotalDays.Should().BeLessThan(180);
    }

    [Fact]
    public async Task ARetentionOfZero_KeepsEverything()
    {
        await SeedAuditAsync(ageInDays: 5_000);

        await RunSweepAsync(new ObservabilityConfig { AuditRetentionDays = 0 });

        await using VortexDbContext db = new(_options);

        (await db.AuditEvents.CountAsync())
            .Should()
            .Be(1, "0 means keep for ever; deleting on 0 would silently erase the whole trail");
    }

    /// <summary>
    /// PERS-RCP-013: nothing aged the commerce journal, and every purchase, prize and settlement
    /// writes to it. What ages out is an operation that finished uneventfully, and its receipts go
    /// with it — they carry no foreign key, so an operation deleted alone leaves rows nothing will
    /// ever collect.
    /// </summary>
    [Fact]
    public async Task AFinishedOperation_AgesOutWithItsReceipts()
    {
        await SeedOperationAsync(CommerceOperationState.Completed, ageInDays: 200);
        await SeedOperationAsync(CommerceOperationState.Completed, ageInDays: 10);

        await RunSweepAsync(new ObservabilityConfig { CommerceRetentionDays = 180 });

        await using VortexDbContext db = new(_options);

        (await db.CommerceOperations.CountAsync()).Should().Be(1, "one was inside the window");
        (await db.CommerceReceipts.CountAsync()).Should().Be(1, "and so was its receipt");
    }

    [Fact]
    public async Task AnOperationNeedingIntervention_IsNeverSwept()
    {
        // It is the operator's work list. Ageing it out is deleting the only record that somebody
        // is owed something.
        await SeedOperationAsync(CommerceOperationState.NeedsIntervention, ageInDays: 5_000);
        await SeedOperationAsync(CommerceOperationState.Pivoted, ageInDays: 5_000);

        await RunSweepAsync(new ObservabilityConfig { CommerceRetentionDays = 1 });

        await using VortexDbContext db = new(_options);

        (await db.CommerceOperations.CountAsync())
            .Should()
            .Be(2, "neither a work item nor an operation still in flight is finished");
    }

    [Fact]
    public async Task AnOperationStillOwingItsEvent_IsKept()
    {
        // The operation row is the outbox. Deleting one before the relay has published loses the
        // event, and with it the quest progress and the daily task it feeds.
        await SeedOperationAsync(
            CommerceOperationState.Completed,
            ageInDays: 5_000,
            relayType: "PurchaseCompletedEvent"
        );

        await RunSweepAsync(new ObservabilityConfig { CommerceRetentionDays = 1 });

        await using VortexDbContext db = new(_options);

        (await db.CommerceOperations.CountAsync()).Should().Be(1);
    }

    private async Task SeedOperationAsync(
        CommerceOperationState state,
        int ageInDays,
        string? relayType = null
    )
    {
        await using VortexDbContext db = new(_options);

        Guid id = Guid.CreateVersion7();
        DateTime at = DateTime.UtcNow.AddDays(-ageInDays);

        db.CommerceOperations.Add(
            new CommerceOperationEntity
            {
                Id = id,
                Kind = CommerceOperationKind.CatalogPurchase,
                PlayerId = 1,
                State = state,
                RelayType = relayType,
                RelayPayload = relayType is null ? null : "{}",
                CreatedAt = at,
                UpdatedAt = at,
            }
        );

        db.CommerceReceipts.Add(
            new CommerceReceiptEntity
            {
                OperationId = id,
                StepKey = CommerceStepKeys.DEBIT,
                CreatedAt = at,
            }
        );

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Seeded in raw SQL for the reason the marketplace tests next door spell out: created_at is
    /// DatabaseGenerated(Identity) and updated_at Computed, so EF never writes either, and
    /// EnsureCreated on SQLite makes both NOT NULL with no default. Deletes are unaffected, which is
    /// all the sweep does.
    /// </summary>
    private async Task SeedAuditAsync(int ageInDays)
    {
        await using VortexDbContext db = new(_options);

        await db.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO audit_events
              (category, action, severity, result, occurred_at, created_at, updated_at)
            VALUES
              ('Room', 'room.entered', 'Info', 'Success',
               datetime('now', {0}), datetime('now'), datetime('now'))
            """,
            $"-{ageInDays} days"
        );
    }

    /// <summary>
    /// Drives one sweep and stops. ExecuteAsync waits five minutes before its first pass, so the
    /// service is started and then cancelled; the sweep itself is invoked directly.
    /// </summary>
    private async Task RunSweepAsync(ObservabilityConfig config)
    {
        using ForensicsRetentionService service = new(
            new TestDbContextFactory(_options),
            Options.Create(config),
            NullLogger<ForensicsRetentionService>.Instance
        );

        await service.SweepOnceAsync(CancellationToken.None);
    }

    private sealed class TestDbContextFactory(DbContextOptions<VortexDbContext> options)
        : IDbContextFactory<VortexDbContext>
    {
        public VortexDbContext CreateDbContext() => new(options);
    }
}
