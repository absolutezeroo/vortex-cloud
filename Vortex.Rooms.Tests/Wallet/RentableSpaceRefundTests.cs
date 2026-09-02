using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Orleans;
using Vortex.Database.Commerce;
using Vortex.Database.Context;
using Vortex.Database.Entities.Catalog;
using Vortex.Database.Entities.Commerce;
using Vortex.Database.Entities.Furniture;
using Vortex.Database.Entities.Room;
using Vortex.Players.Grains;
using Vortex.Primitives.Commerce;
using Vortex.Primitives.Events;
using Vortex.Primitives.Observability;
using Vortex.Primitives.Players.Enums.Wallet;
using Vortex.Primitives.Players.Grains;
using Vortex.Primitives.Players.Wallet;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Tests.Support;
using Xunit;

namespace Vortex.Rooms.Tests.Wallet;

/// <summary>
/// Renting a space moves money three ways -- debit the renter, write the rental, credit the owner --
/// and every failure between them used to leave the renter paying for nothing.
/// </summary>
public sealed class RentableSpaceRefundTests
{
    private const int FURNITURE_ID = 900;
    private const int RENTER_ID = 11;
    private const int OWNER_ID = 22;
    private const int PRICE = 30;

    [Fact]
    public async Task MissingFurnitureRow_IsRefusedBeforeAnyDebit()
    {
        Harness harness = new Harness(seedFurniture: false);

        int? result = await harness
            .Grain.RentAsync(RENTER_ID, CancellationToken.None)
            .ConfigureAwait(true);

        result.Should().Be((int)RentableSpaceRentFailedType.Generic);

        // The lookup used to happen after the debit, so this failure cost the renter the full price.
        harness.Wallet.DebitedAmounts.Should().BeEmpty();
        harness.Wallet.RefundedAmounts.Should().BeEmpty();
    }

    [Fact]
    public async Task MissingStateRowOnUpdate_RefundsTheRenter()
    {
        Harness harness = new Harness(seedFurniture: true);

        // A grain that believes it already has a rental row, with nothing behind it in the database.
        harness.SetExistingSpace(
            new RoomRentableSpaceEntity
            {
                FurnitureEntityId = FURNITURE_ID,
                RentedUntil = null,
                FurnitureEntity = null!,
            }
        );

        Func<Task> act = () => harness.Grain.RentAsync(RENTER_ID, CancellationToken.None);

        await act.Should().ThrowAsync<Exception>().ConfigureAwait(true);

        // It used to fall through silently: renter charged, owner credited, no rental written.
        harness.Wallet.DebitedAmounts.Should().Equal(PRICE);
        harness.Wallet.RefundedAmounts.Should().Equal(PRICE);
        harness.Wallet.OwnerCredits.Should().BeEmpty();
    }

    [Fact]
    public async Task SuccessfulRent_ChargesTheRenterAndPaysTheOwnerOnce()
    {
        Harness harness = new Harness(seedFurniture: true);

        int? result = await harness
            .Grain.RentAsync(RENTER_ID, CancellationToken.None)
            .ConfigureAwait(true);

        result.Should().BeNull();
        harness.Wallet.DebitedAmounts.Should().Equal(PRICE);
        harness.Wallet.RefundedAmounts.Should().BeEmpty();
        harness.Wallet.OwnerCredits.Should().Equal(PRICE);
    }

    /// <summary>
    /// The rental leaves a record of itself, in the state the flow actually ended in.
    /// </summary>
    /// <remarks>
    /// This flow moves value in two directions -- the tenant is debited, the owner is credited --
    /// and it used to do both with no operation id at all. A crash between the two left the tenant
    /// paying an owner who was never paid, with nothing written down to say anything was owed and
    /// nothing to resume from. Completed is the assertion that matters: an operation that stops at
    /// Pivoted is one the escalation sweep has to pick up.
    /// </remarks>
    [Fact]
    public async Task SuccessfulRent_IsRecordedAsACompletedOperation()
    {
        Harness harness = new Harness(seedFurniture: true);

        await harness.Grain.RentAsync(RENTER_ID, CancellationToken.None).ConfigureAwait(true);

        await using VortexDbContext db = harness.NewDbContext();
        CommerceOperationEntity operation = await db.CommerceOperations.SingleAsync();

        operation.Kind.Should().Be(CommerceOperationKind.RentableSpaceRent);
        operation.PlayerId.Should().Be(RENTER_ID);
        operation.State.Should().Be(CommerceOperationState.Completed);
    }

    /// <summary>A rental that never happened leaves no operation claiming it did.</summary>
    [Fact]
    public async Task ARefusedRent_IsNotRecordedAsCompleted()
    {
        Harness harness = new Harness(seedFurniture: true);
        harness.Wallet.DebitSucceeds = false;

        await harness.Grain.RentAsync(RENTER_ID, CancellationToken.None).ConfigureAwait(true);

        await using VortexDbContext db = harness.NewDbContext();

        (await db.CommerceOperations.CountAsync(o => o.State == CommerceOperationState.Completed))
            .Should()
            .Be(0);
    }

    [Fact]
    public async Task NotEnoughCredits_WritesNoRental()
    {
        Harness harness = new Harness(seedFurniture: true);
        harness.Wallet.DebitSucceeds = false;

        int? result = await harness
            .Grain.RentAsync(RENTER_ID, CancellationToken.None)
            .ConfigureAwait(true);

        result.Should().Be((int)RentableSpaceRentFailedType.NotEnoughCredits);
        harness.Wallet.OwnerCredits.Should().BeEmpty();
        harness.RentalRowCount.Should().Be(0);
    }

    private sealed class Harness
    {
        private readonly DbContextOptions<VortexDbContext> _options;

        public Harness(bool seedFurniture)
        {
            _options = new DbContextOptionsBuilder<VortexDbContext>()
                .UseInMemoryDatabase($"rentable-refund-{Guid.NewGuid()}")
                .Options;

            if (seedFurniture)
            {
                using VortexDbContext db = new VortexDbContext(_options);
                db.Furnitures.Add(
                    new FurnitureEntity { Id = FURNITURE_ID, PlayerEntityId = OWNER_ID }
                );
                db.SaveChanges();
            }

            // A real journal over the same database, not a fake: what these tests are about is
            // whether value moved, and the journal is now part of how the answer is recorded.
            Journal = new CommerceJournal(
                new TestDbContextFactory(_options),
                FakeProxy.Create<IVortexMetrics>(_ => null),
                NullLogger<CommerceJournal>.Instance
            );

            Grain = GrainActivationContext.CreateWithIntegerKey<RentableSpaceGrain>(
                FURNITURE_ID,
                new TestDbContextFactory(_options),
                BuildGrainFactory(),
                FakeProxy.Create<IEventPublisher>(_ => null),
                NullLogger<RentableSpaceGrain>.Instance,
                Journal
            );

            SetPrivateField(
                "_terms",
                new RentableSpaceTermsEntity
                {
                    FurnitureEntityId = FURNITURE_ID,
                    Price = PRICE,
                    CurrencyTypeEntityId = 0,
                    RentDurationSeconds = 3600,
                    RequiresHc = false,
                    FurnitureEntity = null!,
                    CurrencyTypeEntity = new CurrencyTypeEntity
                    {
                        Name = "credits",
                        CurrencyType = CurrencyType.Credits,
                        ActivityPointType = null,
                        Enabled = true,
                    },
                }
            );
        }

        public RentableSpaceGrain Grain { get; }

        public CommerceJournal Journal { get; }

        public VortexDbContext NewDbContext() => new(_options);

        public RecordingWallet Wallet { get; } = new();

        public int RentalRowCount
        {
            get
            {
                using VortexDbContext db = new VortexDbContext(_options);

                return db.RoomRentableSpaces.Count();
            }
        }

        /// <summary>Makes the grain take the "update the existing row" branch. The field is loaded
        /// from the database during activation, which these tests deliberately skip.</summary>
        public void SetExistingSpace(RoomRentableSpaceEntity space) =>
            SetPrivateField("_space", space);

        private void SetPrivateField(string name, object value)
        {
            FieldInfo field =
                typeof(RentableSpaceGrain).GetField(
                    name,
                    BindingFlags.Instance | BindingFlags.NonPublic
                ) ?? throw new InvalidOperationException($"RentableSpaceGrain.{name} moved.");

            field.SetValue(Grain, value);
        }

        private IGrainFactory BuildGrainFactory() =>
            FakeProxy.Create<IGrainFactory>(call =>
                call.Method.IsGenericMethod
                && call.Method.GetGenericArguments()[0] == typeof(IPlayerWalletGrain)
                    ? Wallet.AsGrainFor((long)call.Args![0]!)
                    : null
            );
    }

    /// <summary>One recorder for both wallets: which side was charged and which was paid is the
    /// point, so the player id is kept rather than a grain per player.</summary>
    public sealed class RecordingWallet
    {
        public bool DebitSucceeds { get; set; } = true;

        public List<int> DebitedAmounts { get; } = [];

        public List<int> RefundedAmounts { get; } = [];

        public List<int> OwnerCredits { get; } = [];

        public IPlayerWalletGrain AsGrainFor(long playerId) =>
            FakeProxy.Create<IPlayerWalletGrain>(call =>
            {
                switch (call.Method.Name)
                {
                    case nameof(IPlayerWalletGrain.TryDebitAsync):
                        if (!DebitSucceeds)
                        {
                            return Task.FromResult(
                                WalletDebitResult.InsufficientBalance(
                                    new WalletDebitFailure
                                    {
                                        CurrencyKind = new CurrencyKind
                                        {
                                            CurrencyType = CurrencyType.Credits,
                                        },
                                        Amount = PRICE,
                                    }
                                )
                            );
                        }

                        Record(DebitedAmounts, call.Args![0]!);

                        return Task.FromResult(WalletDebitResult.Success());

                    case nameof(IPlayerWalletGrain.CreditBackAsync):
                        Record(RefundedAmounts, call.Args![0]!);

                        return Task.CompletedTask;

                    case nameof(IPlayerWalletGrain.GrantCreditsAsync):
                        if (playerId == OWNER_ID)
                        {
                            OwnerCredits.Add((int)call.Args![0]!);
                        }

                        return Task.CompletedTask;

                    default:
                        return null;
                }
            });

        private static void Record(List<int> sink, object requests)
        {
            foreach (WalletDebitRequest request in (List<WalletDebitRequest>)requests)
            {
                sink.Add(request.Amount);
            }
        }
    }

    private sealed class TestDbContextFactory(DbContextOptions<VortexDbContext> options)
        : IDbContextFactory<VortexDbContext>
    {
        public VortexDbContext CreateDbContext() => new VortexDbContext(options);
    }
}
