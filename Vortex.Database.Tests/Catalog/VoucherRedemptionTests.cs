using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Orleans;
using Orleans.Runtime;
using Vortex.Catalog.Grains;
using Vortex.Database.Context;
using Vortex.Database.Entities.Catalog;
using Vortex.Database.Entities.Players;
using Vortex.Primitives.Catalog.Snapshots;
using Vortex.Primitives.Players;
using Vortex.Primitives.Players.Enums;
using Vortex.Primitives.Players.Enums.Wallet;
using Vortex.Primitives.Players.Grains;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Tests.Support;
using Xunit;

namespace Vortex.Database.Tests.Catalog;

/// <summary>
/// A voucher is currency arriving from nowhere, so the two ways to get it wrong are opposite and
/// both bad: hand it out twice, or take it off the player and grant nothing. The grain claims the
/// redemption before granting, which rules the first out by construction; these hold the second.
/// </summary>
public sealed class VoucherRedemptionTests
{
    private const string CODE = "SUMMER2026";
    private const int PLAYER_ID = 7;

    [Fact]
    public async Task ASecondRedemptionByTheSamePlayer_GrantsNothing()
    {
        Harness harness = await Harness.CreateAsync(amount: 50);

        VoucherRedeemResult first = await harness.RedeemAsync();
        VoucherRedeemResult second = await harness.RedeemAsync();

        first.Success.Should().BeTrue();
        second.Success.Should().BeFalse();
        second.ErrorCode.Should().Be("already_redeemed");
        harness.CreditsGranted.Should().Equal(50);
    }

    [Fact]
    public async Task TheRedemptionCap_IsEnforcedAcrossPlayers()
    {
        Harness harness = await Harness.CreateAsync(amount: 50, maxRedemptions: 1);

        await harness.RedeemAsync(PLAYER_ID);
        VoucherRedeemResult second = await harness.RedeemAsync(PLAYER_ID + 1);

        second.Success.Should().BeFalse();
        second.ErrorCode.Should().Be("max_redemptions_reached");
        harness.CreditsGranted.Should().Equal(50);
    }

    [Fact]
    public async Task AnExpiredVoucher_GrantsNothing()
    {
        Harness harness = await Harness.CreateAsync(
            amount: 50,
            expiresAt: DateTime.UtcNow.AddMinutes(-1)
        );

        VoucherRedeemResult result = await harness.RedeemAsync();

        result.ErrorCode.Should().Be("expired");
        harness.CreditsGranted.Should().BeEmpty();
        harness.RedemptionCount().Should().Be(0);
    }

    [Fact]
    public async Task AnInactiveVoucher_GrantsNothing()
    {
        Harness harness = await Harness.CreateAsync(amount: 50, isActive: false);

        VoucherRedeemResult result = await harness.RedeemAsync();

        result.ErrorCode.Should().Be("inactive");
        harness.CreditsGranted.Should().BeEmpty();
        harness.RedemptionCount().Should().Be(0);
    }

    /// <summary>
    /// An activity-point voucher must pay in the type it names. Paying in credits instead would be
    /// silent: the player gets a balance, just not the one they were promised.
    /// </summary>
    [Fact]
    public async Task AnActivityPointVoucher_PaysTheTypeItNames()
    {
        Harness harness = await Harness.CreateAsync(
            amount: 12,
            currency: CurrencyType.ActivityPoints,
            activityPointType: 5
        );

        await harness.RedeemAsync();

        harness.CreditsGranted.Should().BeEmpty();
        harness.ActivityPointsGranted.Should().Equal((5, 12));
    }

    /// <summary>
    /// The redemption row is written before the currency is granted, so a grant that throws would
    /// leave the voucher burnt and the player paid nothing. Deleting the release in
    /// <c>VoucherGrain.RedeemAsync</c> is the edit this test exists to fail on.
    /// </summary>
    [Fact]
    public async Task AGrantThatFails_ReleasesTheRedemption()
    {
        Harness harness = await Harness.CreateAsync(amount: 50);
        harness.GrantThrows = true;

        VoucherRedeemResult failed = await harness.RedeemAsync();

        failed.Success.Should().BeFalse();
        failed.ErrorCode.Should().Be("grant_failed");
        harness.RedemptionCount().Should().Be(0);

        // and the voucher is still worth something afterwards
        harness.GrantThrows = false;
        VoucherRedeemResult retried = await harness.RedeemAsync();

        retried.Success.Should().BeTrue();
        harness.CreditsGranted.Should().Equal(50);
    }

    /// <summary>
    /// Cancellation is the likeliest reason the grant throws, and releasing under the token that
    /// cancelled it would skip the release in exactly that case.
    /// </summary>
    [Fact]
    public async Task AGrantCancelledMidFlight_StillReleasesTheRedemption()
    {
        Harness harness = await Harness.CreateAsync(amount: 50);
        using CancellationTokenSource cts = new();
        harness.OnGrant = () =>
        {
            cts.Cancel();
            throw new OperationCanceledException(cts.Token);
        };

        VoucherRedeemResult result = await harness.RedeemAsync(PLAYER_ID, cts.Token);

        result.Success.Should().BeFalse();
        harness.RedemptionCount().Should().Be(0);
    }

    private sealed class Harness
    {
        private DbContextOptions<VortexDbContext> _options = null!;
        private VoucherGrain _grain = null!;

        public bool GrantThrows { get; set; }

        public Action? OnGrant { get; set; }

        public System.Collections.Generic.List<int> CreditsGranted { get; } = [];

        public System.Collections.Generic.List<(
            int Type,
            int Amount
        )> ActivityPointsGranted { get; } = [];

        public static async Task<Harness> CreateAsync(
            int amount,
            int? maxRedemptions = null,
            DateTime? expiresAt = null,
            bool isActive = true,
            CurrencyType currency = CurrencyType.Credits,
            int? activityPointType = null
        )
        {
            Harness h = new();
            h._options = new DbContextOptionsBuilder<VortexDbContext>()
                .UseInMemoryDatabase($"voucher-{Guid.NewGuid():N}")
                .Options;

            await using (VortexDbContext db = new(h._options))
            {
                db.Vouchers.Add(
                    new VoucherEntity
                    {
                        Code = CODE,
                        CurrencyType = currency,
                        ActivityPointType = activityPointType,
                        Amount = amount,
                        MaxRedemptions = maxRedemptions,
                        IsActive = isActive,
                        ExpiresAt = expiresAt,
                        CreatedBy = "tests",
                    }
                );
                foreach (int id in new[] { PLAYER_ID, PLAYER_ID + 1 })
                {
                    db.Players.Add(
                        new PlayerEntity
                        {
                            Id = id,
                            Name = $"player{id}",
                            Figure = string.Empty,
                            Gender = AvatarGenderType.Male,
                            PlayerStatus = PlayerStatusType.Offline,
                            PlayerPerks = PlayerPerkFlags.None,
                        }
                    );
                }
                await db.SaveChangesAsync();
            }

            h._grain = new VoucherGrain(
                new TestDbContextFactory(h._options),
                h.BuildGrainFactory(),
                NullLogger<VoucherGrain>.Instance
            );

            SetGrainContext(h._grain, CODE);
            await h._grain.OnActivateAsync(CancellationToken.None);

            return h;
        }

        public Task<VoucherRedeemResult> RedeemAsync(
            int playerId = PLAYER_ID,
            CancellationToken ct = default
        ) => _grain.RedeemAsync(new PlayerId(playerId), ct);

        public int RedemptionCount()
        {
            using VortexDbContext db = new(_options);

            return db.VoucherRedemptions.Count();
        }

        private IGrainFactory BuildGrainFactory()
        {
            IPlayerWalletGrain wallet = FakeProxy.Create<IPlayerWalletGrain>(call =>
            {
                switch (call.Method.Name)
                {
                    case nameof(IPlayerWalletGrain.GrantCreditsAsync):
                        OnGrant?.Invoke();

                        if (GrantThrows)
                        {
                            throw new InvalidOperationException("wallet unreachable");
                        }

                        CreditsGranted.Add((int)call.Args![0]!);

                        return Task.CompletedTask;

                    case nameof(IPlayerWalletGrain.GrantActivityPointsAsync):
                        OnGrant?.Invoke();

                        if (GrantThrows)
                        {
                            throw new InvalidOperationException("wallet unreachable");
                        }

                        ActivityPointsGranted.Add(((int)call.Args![0]!, (int)call.Args![1]!));

                        return Task.CompletedTask;

                    default:
                        return null;
                }
            });

            IPlayerPresenceGrain presence = FakeProxy.Create<IPlayerPresenceGrain>(_ => null);

            return FakeProxy.Create<IGrainFactory>(call =>
                call.Method.IsGenericMethod
                    ? call.Method.GetGenericArguments()[0] switch
                    {
                        Type t when t == typeof(IPlayerWalletGrain) => wallet,
                        Type t when t == typeof(IPlayerPresenceGrain) => presence,
                        _ => null,
                    }
                    : null
            );
        }

        private static void SetGrainContext(Grain grain, string code)
        {
            GrainId grainId = GrainId.Create(GrainType.Create("voucher"), code);

            IGrainContext context = FakeProxy.Create<IGrainContext>(call =>
                call.Method.Name == $"get_{nameof(IGrainContext.GrainId)}" ? grainId : null
            );

            FieldInfo field =
                typeof(Grain).GetField(
                    "<GrainContext>k__BackingField",
                    BindingFlags.Instance | BindingFlags.NonPublic
                ) ?? throw new InvalidOperationException("Grain.GrainContext backing field moved.");

            field.SetValue(grain, context);
        }

        private sealed class TestDbContextFactory(DbContextOptions<VortexDbContext> options)
            : IDbContextFactory<VortexDbContext>
        {
            public VortexDbContext CreateDbContext() => new(options);
        }
    }
}
