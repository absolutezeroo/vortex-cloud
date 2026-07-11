using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Runtime;
using Orleans.Streams;
using Turbo.Database.Context;
using Turbo.Database.Entities.Room;
using Turbo.Logging;
using Turbo.Primitives;
using Turbo.Primitives.Events;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Orleans.Snapshots.Room;
using Turbo.Primitives.Orleans.Snapshots.Room.Settings;
using Turbo.Primitives.Permissions;
using Turbo.Primitives.Pets.Providers;
using Turbo.Primitives.Rooms;
using Turbo.Primitives.Rooms.Enums;
using Turbo.Primitives.Rooms.Events;
using Turbo.Primitives.Rooms.Grains;
using Turbo.Primitives.Rooms.Providers;
using Turbo.Primitives.Rooms.Snapshots;
using Turbo.Rooms.Configuration;
using Turbo.Rooms.Grains.Modules;
using Turbo.Rooms.Grains.Systems;

namespace Turbo.Rooms.Grains;

public sealed partial class RoomGrain : Grain, IRoomGrain
{
    internal readonly IRoomAvatarProvider _avatarProvider;
    internal readonly IDbContextFactory<TurboDbContext> _dbCtxFactory;
    internal readonly IEventPublisher _events;
    internal readonly IGrainFactory _grainFactory;
    internal readonly IRoomItemsProvider _itemsLoader;
    internal readonly ILogger<IRoomGrain> _logger;
    internal readonly IRoomObjectLogicProvider _logicProvider;
    internal readonly IRoomModerationStore _moderationStore;
    internal readonly IPermissionService _permissionService;
    internal readonly IPetLevelProvider _petLevelProvider;
    internal readonly IPetCommandProvider _petCommandProvider;
    internal readonly RoomConfig _roomConfig;
    internal readonly IRoomModelProvider _roomModelProvider;

    internal readonly RoomLiveState _state;
    internal readonly IRoomWiredVariablesProvider _wiredVariablesProvider;
    public readonly RoomActionModule ActionModule;
    public readonly RoomAvatarModule AvatarModule;
    public readonly RoomAvatarTickSystem AvatarTickSystem;
    public readonly RoomChatSystem ChatSystem;

    public readonly RoomEventModule EventModule;
    public readonly RoomFurniModule FurniModule;
    public readonly RoomMapModule MapModule;
    public readonly RoomObjectModule ObjectModule;

    public readonly RoomPathingSystem PathingSystem;
    public readonly RoomPetSystem PetSystem;
    public readonly RoomRollerSystem RollerSystem;
    public readonly RoomSecurityModule SecurityModule;
    public readonly RoomWiredSystem WiredSystem;

    internal IAsyncStream<RoomOutbound> _roomOutbound = default!;

    public RoomGrain(
        IDbContextFactory<TurboDbContext> dbCtxFactory,
        IOptions<RoomConfig> roomConfig,
        ILogger<IRoomGrain> logger,
        IRoomModelProvider roomModelProvider,
        IRoomItemsProvider itemsLoader,
        IRoomObjectLogicProvider logicProvider,
        IRoomAvatarProvider avatarProvider,
        IRoomWiredVariablesProvider wiredVariablesProvider,
        IGrainFactory grainFactory,
        IEventPublisher events,
        IPermissionService permissionService,
        IRoomModerationStore moderationStore,
        IPetLevelProvider petLevelProvider,
        IPetCommandProvider petCommandProvider
    )
    {
        _dbCtxFactory = dbCtxFactory;
        _roomConfig = roomConfig.Value;
        _logger = logger;
        _roomModelProvider = roomModelProvider;
        _itemsLoader = itemsLoader;
        _logicProvider = logicProvider;
        _avatarProvider = avatarProvider;
        _wiredVariablesProvider = wiredVariablesProvider;
        _grainFactory = grainFactory;
        _events = events;
        _permissionService = permissionService;
        _moderationStore = moderationStore;
        _petLevelProvider = petLevelProvider;
        _petCommandProvider = petCommandProvider;

        _state = new RoomLiveState { RoomId = (RoomId)this.GetPrimaryKeyLong() };
        PathingSystem = new RoomPathingSystem(this);
        EventModule = new RoomEventModule(this);
        SecurityModule = new RoomSecurityModule(this);
        MapModule = new RoomMapModule(this);
        ObjectModule = new RoomObjectModule(this);
        AvatarModule = new RoomAvatarModule(this);
        FurniModule = new RoomFurniModule(this);
        ActionModule = new RoomActionModule(this);

        AvatarTickSystem = new RoomAvatarTickSystem(this);
        PetSystem = new RoomPetSystem(this);
        RollerSystem = new RoomRollerSystem(this);
        WiredSystem = new RoomWiredSystem(this);
        ChatSystem = new RoomChatSystem(this);

        EventModule.Register(RollerSystem);
        EventModule.Register(WiredSystem);
    }

    public RoomId RoomId => _state.RoomId;

    public Task DeactivateRoomAsync()
    {
        DeactivateOnIdle();

        return Task.CompletedTask;
    }

    public Task DelayRoomDeactivationAsync()
    {
        DelayDeactivation(TimeSpan.FromMilliseconds(_roomConfig.RoomDeactivationDelayMs));

        return Task.CompletedTask;
    }

    public async Task EnsureRoomActiveAsync(CancellationToken ct)
    {
        await DelayRoomDeactivationAsync().ConfigureAwait(true);

        await MapModule.EnsureMapBuiltAsync(ct);
        await FurniModule.EnsureFurniLoadedAsync(ct);
        await PetSystem.EnsurePetsLoadedAsync(ct);
    }

    public Task<RoomSnapshot> GetSnapshotAsync()
    {
        return Task.FromResult(_state.RoomSnapshot);
    }

    public async Task<RoomSummarySnapshot> GetSummaryAsync()
    {
        int population = await GetRoomPopulationAsync();

        return new RoomSummarySnapshot
        {
            RoomId = _state.RoomSnapshot.RoomId,
            Name = _state.RoomSnapshot.Name,
            Description = _state.RoomSnapshot.Description,
            OwnerId = _state.RoomSnapshot.OwnerId,
            OwnerName = _state.RoomSnapshot.OwnerName,
            Population = population,
            LastUpdatedUtc = DateTime.UtcNow,
        };
    }

    public async Task<int> GetRoomPopulationAsync()
    {
        return await _grainFactory.GetRoomDirectoryGrain().GetRoomPopulationAsync(_state.RoomId);
    }

    public Task<ImmutableArray<KeyValuePair<string, string>>> GetRoomPropertiesAsync() =>
        Task.FromResult(_state.RoomProperties.ToImmutableArray());

    public Task PublishRoomEventAsync(RoomEvent evt, CancellationToken ct)
    {
        return EventModule.PublishAsync(evt, ct);
    }

    public Task SendComposerToRoomAsync(IComposer composer)
    {
        return _roomOutbound.OnNextAsync(
            new RoomOutbound { RoomId = _state.RoomId, Composer = composer }
        );
    }

    public override async Task OnActivateAsync(CancellationToken ct)
    {
        if (_state.EpochMs == 0)
        {
            long now = NowMs();

            _state.EpochMs = now;
            _state.NextAvatarBoundaryMs = AlignToNextBoundary(now, _roomConfig.AvatarTickMs);
            _state.NextPetBoundaryMs = AlignToNextBoundary(now, _roomConfig.Pet.TickMs);
            _state.NextRollerBoundaryMs = AlignToNextBoundary(now, _roomConfig.RollerTickMs);
            _state.NextWiredBoundaryMs = AlignToNextBoundary(now, _roomConfig.WiredTickMs);
        }

        await HydrateRoomStateAsync(ct);
        await HydrateModerationStateAsync(ct);

        await _grainFactory.GetRoomDirectoryGrain().UpsertActiveRoomAsync(_state.RoomSnapshot);

        IStreamProvider? provider = this.GetStreamProvider(
            OrleansStreamProviders.ROOM_STREAM_PROVIDER
        );

        StreamId streamId = StreamId.Create(
            OrleansStreamNames.ROOM_STREAM,
            this.GetPrimaryKeyLong()
        );

        _roomOutbound = provider.GetStream<RoomOutbound>(streamId);

        this.RegisterGrainTimer<object?>(
            async (state, ct) =>
            {
                long now = NowMs();

                await AvatarTickSystem.ProcessAvatarsAsync(now, ct);
                await PetSystem.ProcessPetsAsync(now, ct);
                await WiredSystem.ProcessWiredAsync(now, ct);
                await RollerSystem.ProcessRollersAsync(now, ct);
                await ProcessDoorbellTimeoutsAsync(now, ct);
                await FlushDirtyTilesAsync(ct);
                await FlushDirtyItemsAsync(ct);
            },
            null,
            TimeSpan.FromMilliseconds(_roomConfig.RoomTickMs),
            TimeSpan.FromMilliseconds(_roomConfig.RoomTickMs)
        );
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken ct)
    {
        try
        {
            await FlushDirtyItemsAsync(ct);
            await PetSystem.FlushDirtyPetsAsync(ct);

            await _grainFactory.GetRoomDirectoryGrain().RemoveActiveRoomAsync(_state.RoomId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cleanly deactivate room {RoomId}.", _state.RoomId);
        }
    }

    private async Task HydrateRoomStateAsync(CancellationToken ct)
    {
        TurboDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct);

        try
        {
            RoomEntity entity =
                await dbCtx
                    .Rooms.AsNoTracking()
                    .Include(e => e.GroupEntity)
                    .SingleOrDefaultAsync(e => e.Id == _state.RoomId.Value, ct)
                ?? throw new TurboException(TurboErrorCodeEnum.RoomNotFound);

            _state.Model = _roomModelProvider.GetModelById(entity.RoomModelEntityId);

            _state.RoomSnapshot = new RoomSnapshot
            {
                RoomId = entity.Id,
                Name = entity.Name ?? string.Empty,
                Description = entity.Description ?? string.Empty,
                OwnerId = entity.PlayerEntityId,
                OwnerName = string.Empty,
                Population = 0,
                DoorMode = entity.DoorMode,
                PlayersMax = entity.PlayersMax,
                TradeType = entity.TradeType,
                Score = entity.Score,
                Ranking = 0,
                CategoryId = entity.NavigatorCategoryEntityId ?? -1,
                Tags = RoomTagMapper.ToTags(entity.Tag1, entity.Tag2),
                StaffPick = entity.IsStaffPick,
                AllowBlocking = entity.AllowBlocking,
                AllowPets = entity.AllowPets,
                AllowPetsEat = entity.AllowPetsEat,
                GroupId = entity.GroupEntityId,
                GroupName = entity.GroupEntity?.Name,
                GroupBadge = entity.GroupEntity?.Badge,
                PaintWall = entity.PaintWall,
                PaintFloor = entity.PaintFloor,
                PaintLandscape = entity.PaintLandscape,
                Password = entity.Password ?? string.Empty,
                ModSettings = new ModSettingsSnapshot
                {
                    WhoCanMute = entity.MuteType,
                    WhoCanKick = entity.KickType,
                    WhoCanBan = entity.BanType,
                },
                ChatSettings = new ChatSettingsSnapshot
                {
                    ChatMode = entity.ChatModeType,
                    BubbleWidth = entity.ChatBubbleType,
                    ScrollSpeed = entity.ChatSpeedType,
                    FullHearRange = entity.ChatDistance,
                    FloodSensitivity = entity.ChatFloodType,
                },
                WorldType = _state.Model.Name,
                HideWalls = entity.HideWalls,
                WallThickness = entity.ThicknessWall,
                FloorThickness = entity.ThicknessFloor,
                LastUpdatedUtc = DateTime.UtcNow,
            };

            _state.RoomProperties[RoomPropertyType.WALLPAPER] = entity.PaintWall.ToString(
                CultureInfo.InvariantCulture
            );
            _state.RoomProperties[RoomPropertyType.FLOOR] = entity.PaintFloor.ToString(
                CultureInfo.InvariantCulture
            );
            _state.RoomProperties[RoomPropertyType.LANDSCAPE] = entity.PaintLandscape.ToString(
                CultureInfo.InvariantCulture
            );
            _state.RoomProperties[RoomPropertyType.LANDSCAPEANIM] = "0";

            List<int> rightsPlayerIds = await dbCtx
                .RoomRights.AsNoTracking()
                .Where(r => r.RoomEntityId == entity.Id && r.DeletedAt == null)
                .Select(r => r.PlayerEntityId)
                .ToListAsync(ct)
                .ConfigureAwait(true);

            _state.PlayerIdsWithRights.Clear();

            foreach (int playerId in rightsPlayerIds)
            {
                _state.PlayerIdsWithRights.Add(playerId);
            }
        }
        finally
        {
            await dbCtx.DisposeAsync();
        }
    }

    internal long NowMs()
    {
        return (long)(Stopwatch.GetTimestamp() * 1000.0 / Stopwatch.Frequency);
    }

    internal long AlignToNextBoundary(long now, int offset)
    {
        long delta = now - _state.EpochMs;
        long mod = delta % offset;

        return mod == 0 ? now : now + (offset - mod);
    }

    private async Task HydrateModerationStateAsync(CancellationToken ct)
    {
        IReadOnlyList<RoomMuteRecord> activeMutes = await _moderationStore.GetActiveMutesAsync(
            _state.RoomId.Value,
            ct
        );

        _state.MuteExpiresUtc.Clear();

        foreach (RoomMuteRecord mute in activeMutes)
        {
            _state.MuteExpiresUtc[mute.PlayerId] = mute.ExpiresUtc;
        }
    }
}
