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
using Vortex.Players.Grains;
using Vortex.Primitives.Commerce;
using Vortex.Primitives.Events;
using Vortex.Primitives.Players.Enums.Wallet;
using Vortex.Primitives.Players.Grains;
using Vortex.Primitives.Players.Providers;
using Vortex.Primitives.Players.Snapshots;
using Vortex.Primitives.Players.Wallet;
using Vortex.Tests.Support;
using Xunit;

namespace Vortex.Players.Tests.Wallet;

/// <summary>
/// Orleans is at-most-once and deduplicates nothing durably: a call that timed out after committing
/// looks exactly like one that never ran, and the wallet contract carried no identity at all, so
/// there was nothing to tell the two apart with. A replayed refund credited twice; a debit retried
/// after a timeout charged twice.
/// <para>
/// The rule is that a step is proven idempotent by a replay test rather than assumed —
/// <c>AddEffectAsync</c> inserted unconditionally for as long as it existed, which is what assuming
/// looks like. This is that proof for the wallet.
/// </para>
/// </summary>
/// <remarks>
/// SQLite: the mechanism is a unique index, which the in-memory provider does not enforce. The same
/// test on in-memory would pass and prove nothing.
/// </remarks>
public sealed class WalletReceiptTests : IAsyncLifetime
{
    private const int PLAYER = 31;
    private const int CURRENCY_TYPE_ID = 1;
    private const int STARTING = 500;

    private SqliteConnection _conn = null!;
    private DbContextOptions<VortexDbContext> _options = null!;

    private static List<WalletDebitRequest> Credits(int amount) =>
        [
            new WalletDebitRequest
            {
                CurrencyKind = new CurrencyKind { CurrencyType = CurrencyType.Credits },
                Amount = amount,
            },
        ];

    public async Task InitializeAsync()
    {
        _conn = new SqliteConnection("Filename=:memory:");
        await _conn.OpenAsync();
        _options = new DbContextOptionsBuilder<VortexDbContext>().UseSqlite(_conn).Options;

        await using VortexDbContext db = new(_options);
        await db.Database.EnsureCreatedAsync();
        await db.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = OFF");

        // Raw SQL because created_at is identity-generated and updated_at computed: MySQL fills both
        // from the column defaults the migrations declare, EnsureCreated on SQLite does not.
        await db.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO player_currencies
              (id, player_id, currency_type_id, amount, created_at, updated_at)
            VALUES (1, {0}, {1}, {2}, datetime('now'), datetime('now'))
            """,
            PLAYER,
            CURRENCY_TYPE_ID,
            STARTING
        );
    }

    public async Task DisposeAsync() => await _conn.DisposeAsync();

    /// <summary>The same operation debiting twice is a retry, and charges once.</summary>
    [Fact]
    public async Task ADebitReplayedUnderTheSameOperation_ChargesOnce()
    {
        IPlayerWalletGrain wallet = await BuildAsync();
        CommerceOperationId operation = CommerceOperationId.New();

        (await wallet.TryDebitAsync(Credits(100), operation, CancellationToken.None))
            .Succeeded.Should()
            .BeTrue();
        (await wallet.TryDebitAsync(Credits(100), operation, CancellationToken.None))
            .Succeeded.Should()
            .BeTrue("a replay reports the earlier success rather than failing the caller");

        (await BalanceAsync()).Should().Be(STARTING - 100);
    }

    /// <summary>
    /// A debit the balance cannot cover takes nothing at all.
    /// </summary>
    /// <remarks>
    /// The debit is a conditional UPDATE -- "amount = amount - cost WHERE amount >= cost" -- so the
    /// database is what refuses it rather than a comparison in the grain, which is only sound while
    /// this grain is the wallet's sole writer and nothing in the schema enforces that. Written to
    /// fail loudly if that WHERE ever loses its balance test: without it the subtraction still runs
    /// and the row goes negative.
    /// <para>
    /// Every other "insufficient balance" test in the repository asserts against a fake wallet that
    /// was told to answer InsufficientBalance, so none of them would notice.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task ADebitLargerThanTheBalance_TakesNothing()
    {
        IPlayerWalletGrain wallet = await BuildAsync();

        (
            await wallet.TryDebitAsync(
                Credits(STARTING + 1),
                CommerceOperationId.New(),
                CancellationToken.None
            )
        )
            .Succeeded.Should()
            .BeFalse();

        (await BalanceAsync()).Should().Be(STARTING, "and never goes negative");
    }

    /// <summary>The boundary the condition is written on: spending exactly what is there works.</summary>
    [Fact]
    public async Task ADebitOfTheWholeBalance_Succeeds()
    {
        IPlayerWalletGrain wallet = await BuildAsync();

        (
            await wallet.TryDebitAsync(
                Credits(STARTING),
                CommerceOperationId.New(),
                CancellationToken.None
            )
        )
            .Succeeded.Should()
            .BeTrue();

        (await BalanceAsync()).Should().Be(0);
    }

    /// <summary>Two operations are two purchases, however identical their contents.</summary>
    [Fact]
    public async Task TwoOperationsBuyingTheSameThing_AreChargedSeparately()
    {
        IPlayerWalletGrain wallet = await BuildAsync();

        await wallet.TryDebitAsync(Credits(100), CommerceOperationId.New(), CancellationToken.None);
        await wallet.TryDebitAsync(Credits(100), CommerceOperationId.New(), CancellationToken.None);

        (await BalanceAsync()).Should().Be(STARTING - 200);
    }

    /// <summary>
    /// The refund compensates a pre-pivot purchase that failed, and a compensation running twice is
    /// the same bug as a purchase running twice, pointing the other way.
    /// </summary>
    [Fact]
    public async Task ARefundReplayedUnderTheSameOperation_CreditsOnce()
    {
        IPlayerWalletGrain wallet = await BuildAsync();
        CommerceOperationId operation = CommerceOperationId.New();

        await wallet.TryDebitAsync(Credits(100), operation, CancellationToken.None);

        await wallet.CreditBackAsync(Credits(100), operation, CancellationToken.None);
        await wallet.CreditBackAsync(Credits(100), operation, CancellationToken.None);

        (await BalanceAsync())
            .Should()
            .Be(STARTING, "debited once, refunded once, back where it started");
    }

    /// <summary>
    /// A call with no operation id keeps the old behaviour exactly. The overloads are additive on
    /// purpose: everything that grants outside a commerce flow still uses the old signature, and
    /// migrating all of it in one change is how a wallet refactor becomes a wallet outage.
    /// </summary>
    [Fact]
    public async Task ADebitWithNoOperation_BehavesAsItAlwaysDid()
    {
        IPlayerWalletGrain wallet = await BuildAsync();

        await wallet.TryDebitAsync(Credits(100), CancellationToken.None);
        await wallet.TryDebitAsync(Credits(100), CancellationToken.None);

        (await BalanceAsync())
            .Should()
            .Be(STARTING - 200, "two calls, two debits, no deduplication asked for");
    }

    [Fact]
    public async Task ADebitBeyondTheBalance_IsRefusedAndChangesNothing()
    {
        IPlayerWalletGrain wallet = await BuildAsync();

        WalletDebitResult result = await wallet.TryDebitAsync(
            Credits(STARTING + 1),
            CommerceOperationId.New(),
            CancellationToken.None
        );

        result.Succeeded.Should().BeFalse();
        (await BalanceAsync()).Should().Be(STARTING);
    }

    private async Task<int> BalanceAsync()
    {
        await using VortexDbContext db = new(_options);

        return await db
            .PlayerCurrencies.AsNoTracking()
            .Where(c => c.PlayerEntityId == PLAYER)
            .Select(c => c.Amount)
            .FirstAsync();
    }

    private async Task<IPlayerWalletGrain> BuildAsync()
    {
        PlayerWalletGrain grain = GrainActivationContext.CreateWithIntegerKey<PlayerWalletGrain>(
            PLAYER,
            new TestDbContextFactory(_options),
            BuildCurrencyProvider(),
            BuildGrainFactory(),
            FakeProxy.Create<IEventPublisher>(_ => Task.CompletedTask),
            NullLogger<PlayerWalletGrain>.Instance
        );

        // Activation hydrates the cached balances, and every debit path reads them.
        await grain.OnActivateAsync(CancellationToken.None);

        return grain;
    }

    private static ICurrencyTypeProvider BuildCurrencyProvider()
    {
        CurrencyTypeSnapshot credits = new()
        {
            Id = CURRENCY_TYPE_ID,
            CurrencyType = CurrencyType.Credits,
            ActivityPointType = null,
            Name = "Credits",
            Enabled = true,
            StartingAmount = 0,
        };

        return FakeProxy.Create<ICurrencyTypeProvider>(call =>
            call.Method.Name switch
            {
                nameof(ICurrencyTypeProvider.GetCurrencyType) => credits,
                nameof(ICurrencyTypeProvider.GetCurrencyTypeByKind) => credits,
                _ => null,
            }
        );
    }

    private static IGrainFactory BuildGrainFactory()
    {
        IPlayerPresenceGrain presence = FakeProxy.Create<IPlayerPresenceGrain>(_ =>
            Task.CompletedTask
        );

        return FakeProxy.Create<IGrainFactory>(call =>
            call.Method.IsGenericMethod
            && call.Method.GetGenericArguments()[0] == typeof(IPlayerPresenceGrain)
                ? presence
                : null
        );
    }

    private sealed class TestDbContextFactory(DbContextOptions<VortexDbContext> options)
        : IDbContextFactory<VortexDbContext>
    {
        public VortexDbContext CreateDbContext() => new(options);
    }
}
