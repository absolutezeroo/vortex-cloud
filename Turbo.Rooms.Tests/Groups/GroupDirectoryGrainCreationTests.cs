using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Orleans;
using Turbo.Database.Context;
using Turbo.Database.Entities.Groups;
using Turbo.Database.Entities.Players;
using Turbo.Database.Entities.Room;
using Turbo.Events.Registry;
using Turbo.Players.Grains;
using Turbo.Primitives.Events;
using Turbo.Primitives.Navigator.Enums;
using Turbo.Primitives.Orleans.Snapshots.Players;
using Turbo.Primitives.Players;
using Turbo.Primitives.Players.Enums;
using Turbo.Primitives.Players.Enums.Wallet;
using Turbo.Primitives.Players.Grains;
using Turbo.Primitives.Players.Wallet;
using Turbo.Primitives.Rooms.Enums;
using Turbo.Rooms.Tests.Support;
using Xunit;

namespace Turbo.Rooms.Tests.Groups;

public sealed class GroupDirectoryGrainCreationTests
{
    private const string CORRELATION_ID = "group-create-correlation";

    [Fact]
    public async Task CreateGroupAsync_CancelledCreatingEvent_DoesNotDebitOrWriteRows()
    {
        DbContextOptions<TurboDbContext> options = NewOptions();
        await SeedOwnerRoomAsync(options, ownerId: 7, roomId: 70).ConfigureAwait(true);
        EventTestHarness harness = new EventTestHarness(CORRELATION_ID);
        CancellingGroupCreatingBehavior behavior = new CancellingGroupCreatingBehavior();
        harness.RegisterBehavior<GroupCreatingEvent>(behavior);
        FakeWalletGrain wallet = new FakeWalletGrain();
        FakePlayerGrain player = new FakePlayerGrain();
        GroupDirectoryGrain grain = CreateGrain(options, harness, wallet, player);

        int? groupId = await grain
            .CreateGroupAsync(
                new PlayerId(7),
                "Blocked Guild",
                "blocked",
                1,
                2,
                70,
                new List<int> { 1, 2, 3 },
                CancellationToken.None
            )
            .ConfigureAwait(true);

        groupId.Should().BeNull();
        wallet.DebitCalls.Should().Be(0);
        player.TrackCreditSpendCalls.Should().Be(0);
        behavior.CorrelationId.Should().Be(CORRELATION_ID);

        using TurboDbContext db = new TurboDbContext(options);
        (await db.Groups.CountAsync().ConfigureAwait(true)).Should().Be(0);
        (await db.GroupMembers.CountAsync().ConfigureAwait(true)).Should().Be(0);
        (await db.EconomyLedger.CountAsync().ConfigureAwait(true)).Should().Be(0);
        RoomEntity room = await db.Rooms.SingleAsync().ConfigureAwait(true);
        room.GroupEntityId.Should().BeNull();
    }

    [Fact]
    public async Task CreateGroupAsync_NotCancelled_DebitsAndCreatesGroup()
    {
        DbContextOptions<TurboDbContext> options = NewOptions();
        await SeedOwnerRoomAsync(options, ownerId: 8, roomId: 80).ConfigureAwait(true);
        EventTestHarness harness = new EventTestHarness(CORRELATION_ID);
        CountingGroupCreatedHandler createdHandler = new CountingGroupCreatedHandler();
        harness.RegisterHandler<GroupCreatedEvent>(createdHandler);
        FakeWalletGrain wallet = new FakeWalletGrain();
        FakePlayerGrain player = new FakePlayerGrain();
        GroupDirectoryGrain grain = CreateGrain(options, harness, wallet, player);

        int? groupId = await grain
            .CreateGroupAsync(
                new PlayerId(8),
                "Allowed Guild",
                "allowed",
                3,
                4,
                80,
                new List<int> { 1, 2, 3 },
                CancellationToken.None
            )
            .ConfigureAwait(true);

        groupId.Should().NotBeNull();
        wallet.DebitCalls.Should().Be(1);
        wallet.DebitRequests.Should().ContainSingle();
        wallet.DebitRequests[0].Amount.Should().Be(10);
        wallet.DebitRequests[0].CurrencyKind.CurrencyType.Should().Be(CurrencyType.Credits);
        player.TrackCreditSpendCalls.Should().Be(1);
        createdHandler.Count.Should().Be(1);

        using TurboDbContext db = new TurboDbContext(options);
        GroupEntity group = await db.Groups.SingleAsync().ConfigureAwait(true);
        group.Name.Should().Be("Allowed Guild");
        group.RoomEntityId.Should().Be(80);
        (await db.GroupMembers.CountAsync().ConfigureAwait(true)).Should().Be(1);
        RoomEntity room = await db.Rooms.SingleAsync().ConfigureAwait(true);
        room.GroupEntityId.Should().Be(group.Id);
    }

    private static DbContextOptions<TurboDbContext> NewOptions()
    {
        return new DbContextOptionsBuilder<TurboDbContext>()
            .UseInMemoryDatabase($"group-directory-{Guid.NewGuid():N}")
            .Options;
    }

    private static async Task SeedOwnerRoomAsync(
        DbContextOptions<TurboDbContext> options,
        int ownerId,
        int roomId
    )
    {
        using TurboDbContext db = new TurboDbContext(options);
        PlayerEntity owner = new PlayerEntity
        {
            Id = ownerId,
            Name = $"Owner{ownerId}",
            Figure = "hr-115",
            Gender = AvatarGenderType.Male,
            PlayerStatus = PlayerStatusType.Offline,
            PlayerPerks = PlayerPerkFlags.None,
        };
        RoomModelEntity model = new RoomModelEntity
        {
            Id = roomId + 1000,
            Name = $"model-{roomId}",
            Model = "0",
            DoorX = 0,
            DoorY = 0,
            DoorRotation = Rotation.North,
            Enabled = true,
            Custom = false,
        };
        RoomEntity room = new RoomEntity
        {
            Id = roomId,
            Name = $"Room{roomId}",
            PlayerEntityId = ownerId,
            DoorMode = RoomDoorModeType.Open,
            RoomModelEntityId = model.Id,
            UsersNow = 0,
            PlayersMax = 25,
            PaintWall = 0,
            PaintFloor = 0,
            PaintLandscape = 0,
            WallHeight = -1,
            HideWalls = false,
            ThicknessWall = RoomThicknessType.Normal,
            ThicknessFloor = RoomThicknessType.Normal,
            AllowBlocking = false,
            AllowPets = false,
            AllowPetsEat = false,
            TradeType = RoomTradeModeType.Disabled,
            MuteType = ModSettingType.Owner,
            KickType = ModSettingType.Owner,
            BanType = ModSettingType.Owner,
            ChatModeType = ChatModeType.FreeFlow,
            ChatBubbleType = ChatBubbleWidthType.Normal,
            ChatSpeedType = ChatScrollSpeedType.Normal,
            ChatFloodType = ChatFloodSensitivityType.Minimal,
            ChatDistance = 50,
            Score = 0,
            IsStaffPick = false,
            PlayerEntity = owner,
            RoomModelEntity = model,
        };

        db.Players.Add(owner);
        db.RoomModels.Add(model);
        db.Rooms.Add(room);
        await db.SaveChangesAsync().ConfigureAwait(false);
    }

    private static GroupDirectoryGrain CreateGrain(
        DbContextOptions<TurboDbContext> options,
        EventTestHarness harness,
        FakeWalletGrain wallet,
        FakePlayerGrain player
    )
    {
        IGrainFactory grainFactory = CreateGrainFactory(wallet, player);

        return new GroupDirectoryGrain(
            new TestDbContextFactory(options),
            grainFactory,
            new NullGroupBadgePartProvider(),
            harness.System,
            harness.System,
            NullLogger<GroupDirectoryGrain>.Instance,
            Microsoft.Extensions.Options.Options.Create(new Players.Configuration.GroupConfig())
        );
    }

    private static IGrainFactory CreateGrainFactory(FakeWalletGrain wallet, FakePlayerGrain player)
    {
        IGrainFactory grainFactory = DispatchProxy.Create<IGrainFactory, GrainFactoryProxy>();
        GrainFactoryProxy proxy = (GrainFactoryProxy)(object)grainFactory;
        proxy.Wallet = wallet;
        proxy.Player = player;

        return grainFactory;
    }

    private sealed class TestDbContextFactory(DbContextOptions<TurboDbContext> options)
        : IDbContextFactory<TurboDbContext>
    {
        public TurboDbContext CreateDbContext()
        {
            return new TurboDbContext(options);
        }
    }

    private class GrainFactoryProxy : DispatchProxy
    {
        public FakeWalletGrain Wallet { get; set; } = default!;

        public FakePlayerGrain Player { get; set; } = default!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod is not null && targetMethod.Name == "GetGrain")
            {
                Type grainType = targetMethod.GetGenericArguments()[0];

                if (grainType == typeof(IPlayerWalletGrain))
                {
                    return Wallet;
                }

                if (grainType == typeof(IPlayerGrain))
                {
                    return Player;
                }
            }

            throw new NotSupportedException(targetMethod?.Name);
        }
    }

    private sealed class FakeWalletGrain : IPlayerWalletGrain
    {
        public int DebitCalls { get; private set; }

        public List<WalletDebitRequest> DebitRequests { get; } = new List<WalletDebitRequest>();

        public Task<WalletDebitResult> TryDebitAsync(
            List<WalletDebitRequest> requests,
            CancellationToken ct
        )
        {
            DebitCalls++;
            DebitRequests.AddRange(requests);

            return Task.FromResult(WalletDebitResult.Success());
        }

        public Task CreditBackAsync(List<WalletDebitRequest> requests, CancellationToken ct)
        {
            return Task.CompletedTask;
        }

        public Task<int> GetAmountForCurrencyAsync(CurrencyKind kind, CancellationToken ct)
        {
            return Task.FromResult(0);
        }

        public Task<Dictionary<int, int>> GetActivityPointsAsync(CancellationToken ct)
        {
            return Task.FromResult(new Dictionary<int, int>());
        }

        public Task GrantCreditsAsync(int amount, CancellationToken ct)
        {
            return Task.CompletedTask;
        }

        public Task GrantActivityPointsAsync(
            int activityPointType,
            int amount,
            CancellationToken ct
        )
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakePlayerGrain : IPlayerGrain
    {
        public int TrackCreditSpendCalls { get; private set; }

        public Task TrackCreditSpendAsync(int credits, CancellationToken ct)
        {
            TrackCreditSpendCalls++;

            return Task.CompletedTask;
        }

        public Task SetFigureAsync(string figure, AvatarGenderType gender, CancellationToken ct)
        {
            throw new NotSupportedException();
        }

        public Task SetMottoAsync(string text, CancellationToken ct)
        {
            throw new NotSupportedException();
        }

        public Task<PlayerSummarySnapshot> GetSummaryAsync(CancellationToken ct)
        {
            throw new NotSupportedException();
        }

        public Task<PlayerExtendedProfileSnapshot> GetExtendedProfileSnapshotAsync(
            CancellationToken ct
        )
        {
            throw new NotSupportedException();
        }

        public Task<ClubSubscriptionSnapshot> GetClubSubscriptionAsync(CancellationToken ct)
        {
            throw new NotSupportedException();
        }

        public Task<ClubPurchaseResult> PurchaseClubAsync(
            int months,
            bool isVip,
            int costCredits,
            CancellationToken ct
        )
        {
            throw new NotSupportedException();
        }

        public Task<bool> TryConsumeClubGiftAsync(string productCode, CancellationToken ct)
        {
            throw new NotSupportedException();
        }

        public Task<bool> ApplyAccountBanAsync(
            int actorPlayerId,
            DateTime? bannedUntil,
            string reason,
            CancellationToken ct
        )
        {
            throw new NotSupportedException();
        }

        public Task<bool> ApplyTradingLockAsync(
            int actorPlayerId,
            DateTime? lockedUntil,
            CancellationToken ct
        )
        {
            throw new NotSupportedException();
        }

        public Task<DateTime?> GetActiveBanExpiryAsync(CancellationToken ct)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class CancellingGroupCreatingBehavior : IEventBehavior<GroupCreatingEvent>
    {
        public string? CorrelationId { get; private set; }

        public ValueTask InvokeAsync(
            GroupCreatingEvent env,
            EventContext ctx,
            Func<ValueTask> next,
            CancellationToken ct
        )
        {
            CorrelationId = ctx.CorrelationId;
            ctx.Cancel = true;
            ctx.CancelReason = "blocked";

            return ValueTask.CompletedTask;
        }
    }

    private sealed class CountingGroupCreatedHandler : IEventHandler<GroupCreatedEvent>
    {
        public int Count { get; private set; }

        public ValueTask HandleAsync(GroupCreatedEvent env, EventContext ctx, CancellationToken ct)
        {
            Count++;

            return ValueTask.CompletedTask;
        }
    }
}
