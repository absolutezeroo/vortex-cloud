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
using Vortex.Database.Context;
using Vortex.Database.Entities.Groups;
using Vortex.Database.Entities.Room;
using Vortex.Logging;
using Vortex.Primitives;
using Vortex.Primitives.Events;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Observability;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Orleans.Snapshots.Room;
using Vortex.Primitives.Orleans.Snapshots.Room.Settings;
using Vortex.Primitives.Permissions;
using Vortex.Primitives.Pets.Providers;
using Vortex.Primitives.Rooms;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Events;
using Vortex.Primitives.Rooms.Grains;
using Vortex.Primitives.Rooms.Providers;
using Vortex.Primitives.Rooms.Snapshots;
using Vortex.Rooms.Configuration;
using Vortex.Rooms.Grains.Modules;
using Vortex.Rooms.Grains.Systems;
using Vortex.Rooms.Wired.Logs;

namespace Vortex.Rooms.Grains;

public sealed partial class RoomGrain : Grain, IRoomGrain
{
    /// <summary>How many consecutive failures of one tick step pass between log lines.</summary>
    private const int TickFailureLogInterval = 200;

    /// <summary>Consecutive failure count per tick step, cleared as soon as the step succeeds.</summary>
    private readonly Dictionary<string, int> _tickStepFailures = [];

    internal readonly IRoomAvatarProvider _avatarProvider;
    internal readonly IDbContextFactory<VortexDbContext> _dbCtxFactory;
    internal readonly IEventPublisher _events;
    internal readonly IGrainFactory _grainFactory;
    internal readonly IRoomItemsProvider _itemsLoader;
    internal readonly ILogger<IRoomGrain> _logger;
    internal readonly IRoomObjectLogicProvider _logicProvider;
    internal readonly IRoomModerationStore _moderationStore;
    internal readonly IVortexMetrics _metrics;
    internal readonly IPermissionService _permissionService;
    internal readonly IPetLevelProvider _petLevelProvider;
    internal readonly IPetCommandProvider _petCommandProvider;
    internal readonly IPetVocalProvider _petVocalProvider;
    internal readonly RoomConfig _roomConfig;
    internal readonly IRoomModelProvider _roomModelProvider;

    internal readonly RoomLiveState _state;
    internal readonly RoomWiredLogChannel _wiredLogChannel;
    internal readonly IRoomWiredVariablesProvider _wiredVariablesProvider;
    public readonly RoomActionModule ActionModule;
    public readonly RoomAvatarModule AvatarModule;
    public readonly RoomHandItemModule HandItemModule;
    public readonly RoomAvatarTickSystem AvatarTickSystem;
    public readonly RoomChatSystem ChatSystem;
    public readonly RoomGameChrome GameChrome;
    public readonly RoomGameSystem GameSystem;
    public readonly RoomFreezeSystem FreezeSystem;
    public readonly RoomGameTimerSystem GameTimerSystem;
    public readonly RoomGameScoreboardSystem ScoreboardSystem;

    public readonly RoomEventModule EventModule;
    public readonly RoomFurniModule FurniModule;
    public readonly RoomMapModule MapModule;
    public readonly RoomObjectModule ObjectModule;

    public readonly RoomPathingSystem PathingSystem;
    public readonly RoomPetSystem PetSystem;
    public readonly RoomBotSystem BotSystem;
    public readonly RoomRollerSystem RollerSystem;
    public readonly RoomSecurityModule SecurityModule;
    public readonly RoomWiredSystem WiredSystem;
    public readonly RoomModerationSystem ModerationSystem;
    public readonly RoomMysteryBoxSystem MysteryBoxSystem;
    public readonly RoomCrackableSystem CrackableSystem;
    public readonly RoomTradingSystem TradingSystem;

    internal IAsyncStream<RoomOutbound> _roomOutbound = default!;

    public RoomGrain(
        IDbContextFactory<VortexDbContext> dbCtxFactory,
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
        IVortexMetrics metrics,
        IRoomModerationStore moderationStore,
        IPetLevelProvider petLevelProvider,
        IPetCommandProvider petCommandProvider,
        IPetVocalProvider petVocalProvider,
        RoomWiredLogChannel wiredLogChannel
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
        _metrics = metrics;
        _moderationStore = moderationStore;
        _petLevelProvider = petLevelProvider;
        _petCommandProvider = petCommandProvider;
        _petVocalProvider = petVocalProvider;
        _wiredLogChannel = wiredLogChannel;

        _state = new RoomLiveState { RoomId = (RoomId)this.GetPrimaryKeyLong() };
        PathingSystem = new RoomPathingSystem(this);
        EventModule = new RoomEventModule(this);
        SecurityModule = new RoomSecurityModule(this);
        MapModule = new RoomMapModule(this);
        ObjectModule = new RoomObjectModule(this);
        AvatarModule = new RoomAvatarModule(this);
        HandItemModule = new RoomHandItemModule(this);
        FurniModule = new RoomFurniModule(this);
        ActionModule = new RoomActionModule(this);

        AvatarTickSystem = new RoomAvatarTickSystem(this);
        PetSystem = new RoomPetSystem(this);
        BotSystem = new RoomBotSystem(this);
        RollerSystem = new RoomRollerSystem(this);
        WiredSystem = new RoomWiredSystem(this);
        ChatSystem = new RoomChatSystem(this);
        // Chrome before GameSystem, GameSystem before any game: RoomFreezeSystem's field
        // initializer reads GameSystem.TeamState, and every game system reads GameChrome.
        GameChrome = new RoomGameChrome(this);
        GameSystem = new RoomGameSystem(this);
        FreezeSystem = new RoomFreezeSystem(this);
        GameTimerSystem = new RoomGameTimerSystem(this);
        ScoreboardSystem = new RoomGameScoreboardSystem(this);
        ModerationSystem = new RoomModerationSystem(this);
        MysteryBoxSystem = new RoomMysteryBoxSystem(this);
        CrackableSystem = new RoomCrackableSystem(this);
        TradingSystem = new RoomTradingSystem(this);

        // Every game the room can host plugs in here, and nowhere else: the game-timer furni, the wired
        // control-clock action, the tick loop and the avatar-left path all go through GameSystem and
        // never name a game. Battle Banzai, football and hockey each land as one more line.
        GameSystem.Register(FreezeSystem);

        EventModule.Register(RollerSystem);
        EventModule.Register(WiredSystem);
        // The scoreboard brick paints every game's score displays from the same events the wired
        // boxes read, so no game refreshes a board by hand.
        EventModule.Register(ScoreboardSystem);
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
        using (
            _metrics.MeasureRoomDirectoryCall(nameof(IRoomDirectoryGrain.GetRoomPopulationAsync))
        )
        {
            return await _grainFactory
                .GetRoomDirectoryGrain()
                .GetRoomPopulationAsync(_state.RoomId);
        }
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
            _state.NextBotBoundaryMs = now;
            _state.NextRollerBoundaryMs = AlignToNextBoundary(now, _roomConfig.RollerTickMs);
            _state.NextWiredBoundaryMs = AlignToNextBoundary(now, _roomConfig.WiredTickMs);
        }

        await HydrateRoomStateAsync(ct);
        await HydrateModerationStateAsync(ct);

        using (_metrics.MeasureRoomDirectoryCall(nameof(IRoomDirectoryGrain.UpsertActiveRoomAsync)))
        {
            await _grainFactory.GetRoomDirectoryGrain().UpsertActiveRoomAsync(_state.RoomSnapshot);
        }

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
                // Read once: at 20 ticks a second per room, a disabled metrics stack must cost this
                // boolean and nothing else -- no timestamps taken, no elapsed time computed.
                bool measured = _metrics.Enabled;
                long tickStartedAt = measured ? Stopwatch.GetTimestamp() : 0L;

                long now = NowMs();

                // Each step is isolated. Run bare, one throw -- a single malformed wired item, one
                // pet with a broken path -- aborted the whole tick, and since the two flushes are
                // last, the room also stopped persisting furniture and tile changes for as long as
                // the fault lasted.
                await RunTickStepAsync(
                    "avatars",
                    () => AvatarTickSystem.ProcessAvatarsAsync(now, ct)
                );
                await RunTickStepAsync("pets", () => PetSystem.ProcessPetsAsync(now, ct));
                await RunTickStepAsync("bots", () => BotSystem.ProcessBotsAsync(now, ct));
                await RunTickStepAsync("wired", () => WiredSystem.ProcessWiredAsync(now, ct));
                await RunTickStepAsync("rollers", () => RollerSystem.ProcessRollersAsync(now, ct));
                await RunTickStepAsync("game-timer", () => GameTimerSystem.ProcessAsync(now, ct));

                foreach (IRoomMinigame minigame in GameSystem.Minigames)
                {
                    await RunTickStepAsync(minigame.Name, () => minigame.TickAsync(now, ct));
                }

                await RunTickStepAsync("doorbell", () => ProcessDoorbellTimeoutsAsync(now, ct));
                await RunTickStepAsync(
                    "mystery-box",
                    () => ProcessMysteryBoxTimeoutsAsync(now, ct)
                );
                await RunTickStepAsync("flush-tiles", () => FlushDirtyTilesAsync(ct));
                await RunTickStepAsync("flush-items", () => FlushDirtyItemsAsync(ct));

                if (measured)
                {
                    _metrics.RoomTickCompleted(
                        Stopwatch.GetElapsedTime(tickStartedAt).TotalMilliseconds
                    );
                }
            },
            null,
            TimeSpan.FromMilliseconds(_roomConfig.RoomTickMs),
            TimeSpan.FromMilliseconds(_roomConfig.RoomTickMs)
        );
    }

    /// <summary>
    /// Runs one tick step, keeping its failure to itself. A step that throws must not take the rest
    /// of the tick down with it -- least of all the two flushes at the end, which are what actually
    /// persist the room.
    /// </summary>
    private async Task RunTickStepAsync(string step, Func<Task> stepAsync)
    {
        bool measured = _metrics.Enabled;
        long startedAt = measured ? Stopwatch.GetTimestamp() : 0L;

        try
        {
            await stepAsync().ConfigureAwait(true);

            _tickStepFailures.Remove(step);
        }
        catch (OperationCanceledException)
        {
            // The grain is going away; not a step failure, and not a duration worth recording -- the
            // step was cut short rather than finished.
            throw;
        }
        catch (Exception ex)
        {
            // At 50ms a permanently broken step would emit twenty lines a second, per room, for as
            // long as the fault lasts, so only the first failure and then one in every
            // TickFailureLogInterval are written. The count doubles as "how long has this been
            // broken", which is the part worth reading.
            int failures = _tickStepFailures.GetValueOrDefault(step) + 1;
            _tickStepFailures[step] = failures;

            if (failures == 1 || failures % TickFailureLogInterval == 0)
            {
                _logger.LogError(
                    ex,
                    "Room tick step {Step} failed in room {RoomId} ({FailureCount} consecutive); the rest of the tick still ran.",
                    step,
                    _state.RoomId.Value,
                    failures
                );
            }
        }

        // A step that threw is still a step that consumed tick budget, so it is timed like any other;
        // only the cancellation path above escapes, and it does so by rethrowing.
        if (measured)
        {
            _metrics.RoomTickStepCompleted(
                step,
                Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds
            );
        }
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken ct)
    {
        try
        {
            await FlushDirtyItemsAsync(ct);
            await PetSystem.FlushDirtyPetsAsync(ct);

            using (
                _metrics.MeasureRoomDirectoryCall(nameof(IRoomDirectoryGrain.RemoveActiveRoomAsync))
            )
            {
                await _grainFactory.GetRoomDirectoryGrain().RemoveActiveRoomAsync(_state.RoomId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cleanly deactivate room {RoomId}.", _state.RoomId);
        }
    }

    private async Task HydrateRoomStateAsync(CancellationToken ct)
    {
        VortexDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct);

        try
        {
            RoomEntity entity =
                await dbCtx
                    .Rooms.AsNoTracking()
                    .Include(e => e.GroupEntity)
                    .Include(e => e.PlayerEntity)
                    .SingleOrDefaultAsync(e => e.Id == _state.RoomId.Value, ct)
                ?? throw new VortexException(VortexErrorCodeEnum.RoomNotFound);

            _state.Model = _roomModelProvider.GetModelById(entity.RoomModelEntityId);

            _state.RoomSnapshot = new RoomSnapshot
            {
                RoomId = entity.Id,
                Name = entity.Name ?? string.Empty,
                Description = entity.Description ?? string.Empty,
                OwnerId = entity.PlayerEntityId,
                OwnerName = entity.PlayerEntity?.Name ?? string.Empty,
                LeaveOnDoorTile = entity.LeaveOnDoorTile,
                IdleSleepEnabled = entity.IdleSleepEnabled,
                IdleSleepTimeoutSeconds = entity.IdleSleepTimeoutSeconds,
                IdleAutokickEnabled = entity.IdleAutokickEnabled,
                IdleAutokickTimeoutSeconds = entity.IdleAutokickTimeoutSeconds,
                MuteAllPets = entity.MuteAllPets,
                Population = 0,
                DoorMode = entity.DoorMode,
                PlayersMax = entity.PlayersMax,
                MaxVisitorsLimit = _roomConfig.MaxVisitorsLimit,
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
                PaintWall = entity.PaintWall ?? string.Empty,
                PaintFloor = entity.PaintFloor ?? string.Empty,
                PaintLandscape = entity.PaintLandscape ?? string.Empty,
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

            _state.RoomProperties[RoomPropertyType.WALLPAPER] = DecorationOrDefault(
                entity.PaintWall
            );
            _state.RoomProperties[RoomPropertyType.FLOOR] = DecorationOrDefault(entity.PaintFloor);
            _state.RoomProperties[RoomPropertyType.LANDSCAPE] = DecorationOrDefault(
                entity.PaintLandscape
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

            await HydrateGroupMembershipAsync(dbCtx, entity.GroupEntityId, ct).ConfigureAwait(true);
        }
        finally
        {
            await dbCtx.DisposeAsync();
        }
    }

    /// <summary>
    /// A room property the client can read but that no decoration has been applied to yet. The
    /// client treats "0" as "the default surface"; an empty string would render as a missing asset.
    /// </summary>
    private static string DecorationOrDefault(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "0" : value;

    /// <summary>
    /// Loads the owning guild's membership roster and decoration policy into live state so
    /// <see cref="Modules.RoomSecurityModule"/> can resolve group controller levels without a DB hit
    /// per check. A no-op (and a full reset) for rooms that are not a guild base.
    /// </summary>
    private async Task HydrateGroupMembershipAsync(
        VortexDbContext dbCtx,
        int? groupId,
        CancellationToken ct
    )
    {
        _state.GroupMemberRanks.Clear();
        _state.GroupAdminOnlyDecoration = false;

        if (groupId is not int id)
        {
            return;
        }

        _state.GroupAdminOnlyDecoration = await dbCtx
            .Groups.AsNoTracking()
            .Where(g => g.Id == id && g.DeletedAt == null)
            .Select(g => g.AdminOnlyDecoration)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(true);

        List<GroupMemberEntity> members = await dbCtx
            .GroupMembers.AsNoTracking()
            .Where(m => m.GroupEntityId == id && m.DeletedAt == null)
            .ToListAsync(ct)
            .ConfigureAwait(true);

        foreach (GroupMemberEntity member in members)
        {
            _state.GroupMemberRanks[member.PlayerEntityId] = member.Rank;
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

    /// <summary>
    /// The first tick boundary strictly after <paramref name="now"/>, on the same epoch grid a tick
    /// clock walks.
    /// <para>
    /// Every tick system used to catch up by stepping — <c>while (now >= next) next += tick</c> —
    /// which is exact and cheap for the millisecond drift a running room sees. It is neither when
    /// the gap is large: a room that was paused, a host that slept, a clock that jumped, and the
    /// step count becomes millions, spent inside the grain's single turn with the whole room
    /// waiting behind it. Arriving at the same answer in one step removes that cliff without
    /// changing where the boundaries fall.
    /// </para>
    /// </summary>
    internal long AdvanceBoundaryPast(long now, int tickMs)
    {
        long aligned = AlignToNextBoundary(now, tickMs);

        // AlignToNextBoundary answers "now" when now is itself a boundary; a clock has to move past
        // the boundary it just fired, or the same tick runs again.
        return aligned == now ? now + tickMs : aligned;
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
