using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Orleans;
using Vortex.Database.Context;
using Vortex.Database.Entities.Marketplace;
using Vortex.Marketplace.Grains;
using Vortex.Primitives.Events;
using Vortex.Primitives.Inventory.Grains;
using Vortex.Primitives.Marketplace.Providers;
using Vortex.Primitives.Marketplace.Snapshots;
using Vortex.Primitives.Players.Grains;
using Vortex.Primitives.Players.Wallet;
using Vortex.Tests.Support;
using Xunit;

namespace Vortex.Database.Tests.Commerce;

/// <summary>
/// The three marketplace flows that commit one half of a transfer before attempting the other.
/// <c>MarketplaceClaimRaceTests</c> already covers Buy — its <c>ExecuteUpdate</c> claim is a correct
/// concurrency pivot and stays. These are the other three, where the ordering is simply backwards.
/// <para>
/// Characterisation. Each records what today's code leaves behind when the second half fails, and
/// names the PR that flips it.
/// </para>
/// </summary>
/// <remarks>
/// SQLite rather than the in-memory provider: Buy claims with ExecuteUpdate, which in-memory does
/// not implement, and keeping the whole marketplace suite on one provider keeps the fixtures shared.
/// Rows are seeded in raw SQL because created_at is identity-generated and updated_at computed —
/// MySQL fills both from column defaults the migrations declare, EnsureCreated on SQLite does not.
/// </remarks>
public sealed class MarketplaceWindowTests : IAsyncLifetime
{
    private const int SELLER = 3;
    private const int OFFER_ID = 700;
    private const int DEFINITION_ID = 42;
    private const int PRICE = 100;
    private const int ITEM_ID = 900;

    private SqliteConnection _conn = null!;
    private DbContextOptions<VortexDbContext> _options = null!;

    private readonly List<int> _granted = [];
    private readonly List<int> _creditsGranted = [];
    private readonly List<int> _removed = [];

    private bool _grantThrows;
    private bool _creditThrows;
    private bool _offerInsertThrows;

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

    private async Task SeedOfferAsync(MarketplaceOfferState state, int creditsOwed)
    {
        await using VortexDbContext db = new(_options);

        await db.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO marketplace_offers
              (id, seller_id, definition_id, sprite_id, furni_type, extra_data, price, state,
               credits_owed, expires_at, created_at, updated_at)
            VALUES
              ({0}, {1}, {2}, 7, 1, '', {3}, {4}, {5}, datetime('now', '+1 day'),
               datetime('now'), datetime('now'))
            """,
            OFFER_ID,
            SELLER,
            DEFINITION_ID,
            PRICE,
            (int)state,
            creditsOwed
        );
    }

    private async Task<MarketplaceOfferEntity?> OfferAsync()
    {
        await using VortexDbContext db = new(_options);

        return await db
            .MarketplaceOffers.AsNoTracking()
            .SingleOrDefaultAsync(o => o.Id == OFFER_ID);
    }

    /// <summary>
    /// WINDOW — MakeOffer removes the item from the inventory first and inserts the offer second. A
    /// failure in between destroys the furniture: it is gone from the inventory and there is no
    /// offer holding it.
    /// </summary>
    /// <remarks>
    /// Flipped by PR-C5, where the offer is inserted PendingRemoval first and only then does the
    /// item leave the inventory.
    /// </remarks>
    [Fact]
    public async Task AnOfferInsertThatFails_LeavesTheItemRemovedAndUnlisted()
    {
        _offerInsertThrows = true;

        Func<Task> act = () => BuildGrain().MakeOfferAsync(ITEM_ID, PRICE, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();

        _removed
            .Should()
            .Equal([ITEM_ID], "the item left the inventory before anything was listed");

        await using VortexDbContext db = new(_options);
        (await db.MarketplaceOffers.CountAsync()).Should().Be(0, "and nothing holds it now");
    }

    /// <summary>
    /// WINDOW — Cancel commits the Cancelled state and then gives the item back. A failure in
    /// between leaves an offer that is neither for sale nor returned: the furniture is gone.
    /// </summary>
    /// <remarks>Flipped by PR-C5: Cancelled becomes a journalled pivot and the restitution a
    /// replayable step.</remarks>
    [Fact]
    public async Task ARestitutionThatFails_LeavesTheOfferCancelledAndTheItemGone()
    {
        await SeedOfferAsync(MarketplaceOfferState.Active, creditsOwed: 0);
        _grantThrows = true;

        Func<Task> act = () =>
            BuildGrain().CancelOrRedeemOfferAsync(OFFER_ID, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();

        MarketplaceOfferEntity offer = (await OfferAsync())!;
        offer.State.Should().Be(MarketplaceOfferState.Cancelled, "the state was committed first");
        _granted.Should().BeEmpty("and the item never came back");
    }

    /// <summary>
    /// WINDOW — Redeem zeroes what the seller is owed and then credits the wallet. A failure in
    /// between is the seller's money: the debt is settled in the database and the credits never
    /// arrived.
    /// </summary>
    /// <remarks>Flipped by PR-C5, where the zeroing and the credit are tied by a receipt.</remarks>
    [Fact]
    public async Task ACreditThatFails_LeavesTheDebtSettledAndUnpaid()
    {
        await SeedOfferAsync(MarketplaceOfferState.Sold, creditsOwed: PRICE - 1);
        _creditThrows = true;

        Func<Task> act = () => BuildGrain().RedeemCreditsAsync(CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();

        MarketplaceOfferEntity offer = (await OfferAsync())!;
        offer.CreditsOwed.Should().Be(0, "the debt was zeroed before the credit was attempted");
        _creditsGranted.Should().BeEmpty("and the seller was never paid");
    }

    [Fact]
    public async Task ARedeemThatSucceeds_PaysTheSellerAndClearsTheDebt()
    {
        await SeedOfferAsync(MarketplaceOfferState.Sold, creditsOwed: PRICE - 1);

        (await BuildGrain().RedeemCreditsAsync(CancellationToken.None)).Should().Be(PRICE - 1);

        (await OfferAsync())!.CreditsOwed.Should().Be(0);
        _creditsGranted.Should().Equal([PRICE - 1]);
    }

    private MarketplacePurchaseGrain BuildGrain()
    {
        MarketplacePurchaseGrain grain = new(
            new TestDbContextFactory(_options, () => _offerInsertThrows),
            BuildGrainFactory(),
            FakeProxy.Create<IMarketplaceSettingsProvider>(call =>
                call.Method.Name == nameof(IMarketplaceSettingsProvider.GetSettings)
                    ? new MarketplaceSettingsSnapshot
                    {
                        CommissionPercent = 1,
                        OfferDurationSeconds = 3600,
                    }
                    : null
            ),
            FakeProxy.Create<IEventPublisher>(_ => Task.CompletedTask),
            NullLogger<MarketplacePurchaseGrain>.Instance
        );

        GrainContexts.Install(grain, "marketplacepurchase", SELLER);

        return grain;
    }

    private IGrainFactory BuildGrainFactory()
    {
        IInventoryGrain inventory = FakeProxy.Create<IInventoryGrain>(call =>
        {
            switch (call.Method.Name)
            {
                case nameof(IInventoryGrain.GetItemSnapshotAsync):
                    return Task.FromResult<Vortex.Primitives.Inventory.Snapshots.FurnitureItemSnapshot?>(
                        MarketplaceItems.Snapshot(ITEM_ID, DEFINITION_ID, SELLER)
                    );

                case nameof(IInventoryGrain.RemoveFurnitureAsync):
                    _removed.Add(
                        ((Vortex.Primitives.Rooms.Object.RoomObjectId)call.Args![0]!).Value
                    );

                    return Task.FromResult(true);

                case nameof(IInventoryGrain.GrantFurnitureDefinitionAsync):
                    if (_grantThrows)
                    {
                        throw new InvalidOperationException("inventory unreachable");
                    }

                    _granted.Add((int)call.Args![0]!);

                    return Task.CompletedTask;

                default:
                    return null;
            }
        });

        IPlayerWalletGrain wallet = FakeProxy.Create<IPlayerWalletGrain>(call =>
        {
            switch (call.Method.Name)
            {
                case nameof(IPlayerWalletGrain.GrantCreditsAsync):
                    if (_creditThrows)
                    {
                        throw new InvalidOperationException("wallet unreachable");
                    }

                    _creditsGranted.Add((int)call.Args![0]!);

                    return Task.CompletedTask;

                default:
                    return null;
            }
        });

        return FakeProxy.Create<IGrainFactory>(call =>
            call.Method.IsGenericMethod
                ? call.Method.GetGenericArguments()[0] switch
                {
                    Type t when t == typeof(IInventoryGrain) => inventory,
                    Type t when t == typeof(IPlayerWalletGrain) => wallet,
                    _ => null,
                }
                : null
        );
    }

    /// <summary>
    /// A context factory that can hand out a context whose SaveChanges throws — the only way to make
    /// the offer insert fail where MakeOffer actually performs it.
    /// </summary>
    private sealed class TestDbContextFactory(
        DbContextOptions<VortexDbContext> options,
        Func<bool> insertThrows
    ) : IDbContextFactory<VortexDbContext>
    {
        public VortexDbContext CreateDbContext() =>
            insertThrows() ? new ThrowingDbContext(options) : new VortexDbContext(options);
    }

    private sealed class ThrowingDbContext(DbContextOptions<VortexDbContext> options)
        : VortexDbContext(options)
    {
        public override Task<int> SaveChangesAsync(CancellationToken ct = default) =>
            throw new InvalidOperationException("the database went away mid-insert");
    }
}

internal static class MarketplaceItems
{
    public static Vortex.Primitives.Inventory.Snapshots.FurnitureItemSnapshot Snapshot(
        int itemId,
        int definitionId,
        int ownerId
    ) =>
        new()
        {
            ItemId = new Vortex.Primitives.Rooms.Object.RoomObjectId(itemId),
            OwnerId = new Vortex.Primitives.Players.PlayerId(ownerId),
            OwnerName = "seller",
            RoomId = new Vortex.Primitives.Rooms.RoomId(0),
            Definition = InventoryGrainFixture.Definition(definitionId),
            SpriteId = 7,
            ExtraData = string.Empty,
            StuffData = new Vortex.Primitives.Furniture.Snapshots.StuffData.LegacyStuffSnapshot
            {
                StuffBitmask = 0,
                Data = string.Empty,
            },
            SecondsToExpiration = 0,
            HasRentPeriodStarted = false,
        };
}
