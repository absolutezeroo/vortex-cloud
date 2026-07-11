using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Turbo.Database.Context;
using Turbo.Primitives.Action;
using Turbo.Primitives.Messages.Outgoing.Navigator;
using Turbo.Primitives.Messages.Outgoing.Room.Action;
using Turbo.Primitives.Messages.Outgoing.Room.Engine;
using Turbo.Primitives.Messages.Outgoing.Room.Layout;
using Turbo.Primitives.Messages.Outgoing.Room.Permissions;
using Turbo.Primitives.Messages.Outgoing.Room.Session;
using Turbo.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredmenu;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Orleans.Snapshots.Room;
using Turbo.Primitives.Players;
using Turbo.Primitives.Players.Grains;
using Turbo.Primitives.Rooms;
using Turbo.Primitives.Rooms.Enums;
using Turbo.Primitives.Rooms.Grains;
using Turbo.Primitives.Rooms.Object;
using Turbo.Primitives.Rooms.Snapshots.Avatars;
using Turbo.Primitives.Rooms.Snapshots.Furniture;
using Turbo.Primitives.Rooms.Snapshots.Mapping;
using Turbo.Rooms.Configuration;

namespace Turbo.Rooms;

internal sealed partial class RoomService(
    ILogger<IRoomService> logger,
    IOptions<RoomConfig> roomConfig,
    ISessionGateway sessionGateway,
    IGrainFactory grainFactory,
    IDbContextFactory<TurboDbContext> dbContextFactory,
    IRoomModerationStore roomModerationStore
) : IRoomService
{
    private readonly IDbContextFactory<TurboDbContext> _dbContextFactory = dbContextFactory;
    private readonly IGrainFactory _grainFactory = grainFactory;
    private readonly ILogger<IRoomService> _logger = logger;
    private readonly RoomConfig _roomConfig = roomConfig.Value;
    private readonly IRoomModerationStore _roomModerationStore = roomModerationStore;
    private readonly ISessionGateway _sessionGateway = sessionGateway;

    public async Task OpenRoomForPlayerIdAsync(
        ActionContext ctx,
        PlayerId playerId,
        RoomId roomId,
        CancellationToken ct,
        string password = ""
    )
    {
        IPlayerPresenceGrain playerPresence = _grainFactory.GetPlayerPresenceGrain(playerId);
        RoomPendingSnapshot pendingRoom = await playerPresence
            .GetPendingRoomAsync()
            .ConfigureAwait(false);

        if (
            await _roomModerationStore
                .IsBannedAsync(roomId.Value, playerId.Value, ct)
                .ConfigureAwait(false)
        )
        {
            await playerPresence
                .SendComposerAsync(
                    new CantConnectMessageComposer { ErrorType = RoomConnectionErrorType.Banned }
                )
                .ConfigureAwait(false);
            return;
        }

        if (pendingRoom.RoomId == roomId)
        {
            return;
        }

        await playerPresence.ClearActiveRoomAsync(ct).ConfigureAwait(false);
        await playerPresence.SetPendingRoomAsync(roomId, true).ConfigureAwait(false);

        IRoomGrain room = _grainFactory.GetRoomGrain(roomId);

        await playerPresence
            .SendComposerAsync(new OpenConnectionMessageComposer { RoomId = roomId })
            .ConfigureAwait(false);

        await room.EnsureRoomActiveAsync(ct).ConfigureAwait(false);

        RoomSnapshot snapshot = await room.GetSnapshotAsync().ConfigureAwait(false);
        RoomControllerType controllerLevel = await room.GetControllerLevelAsync(ctx, ct)
            .ConfigureAwait(false);
        bool isOwner = controllerLevel == RoomControllerType.Owner;
        ImmutableArray<RoomAvatarSnapshot> avatarSnapshots = await room.GetAllAvatarSnapshotsAsync(
                ct
            )
            .ConfigureAwait(false);

        if (controllerLevel < RoomControllerType.Rights)
        {
            RoomConnectionErrorType? rejection =
                snapshot.PlayersMax > 0 && avatarSnapshots.Length >= snapshot.PlayersMax
                    ? RoomConnectionErrorType.RoomFull
                : snapshot.DoorMode == RoomDoorModeType.Locked ? RoomConnectionErrorType.NoEntry
                : snapshot.DoorMode == RoomDoorModeType.Password
                && !string.Equals(snapshot.Password, password, StringComparison.Ordinal)
                    ? RoomConnectionErrorType.NoEntry
                : null;

            if (rejection is not null)
            {
                await playerPresence
                    .SendComposerAsync(
                        new CantConnectMessageComposer { ErrorType = rejection.Value }
                    )
                    .ConfigureAwait(false);
                await playerPresence
                    .SetPendingRoomAsync(RoomId.Invalid, false)
                    .ConfigureAwait(false);
                return;
            }
        }

        RoomMapSnapshot mapSnapshot = await room.GetMapSnapshotAsync(ct).ConfigureAwait(false);

        await playerPresence
            .SendComposerAsync(
                new RoomReadyMessageComposer { WorldType = snapshot.WorldType, RoomId = roomId },
                new RoomRatingMessageComposer { Rating = 0, CanRate = false },
                new RoomEntryTileMessageComposer
                {
                    X = mapSnapshot.DoorX,
                    Y = mapSnapshot.DoorY,
                    Rotation = mapSnapshot.DoorRotation,
                },
                new HeightMapMessageComposer
                {
                    Width = mapSnapshot.Width,
                    Size = mapSnapshot.Size,
                    Heights = mapSnapshot.TileEncodedHeights,
                },
                new FloorHeightMapMessageComposer
                {
                    ScaleType = _roomConfig.DefaultRoomScale,
                    FixedWallsHeight = _roomConfig.DefaultWallHeight,
                    ModelData = mapSnapshot.ModelData,
                    AreaHideData = [],
                },
                new RoomVisualizationSettingsMessageComposer
                {
                    WallsHidden = snapshot.HideWalls,
                    WallThickness = snapshot.WallThickness,
                    FloorThickness = snapshot.FloorThickness,
                }
            )
            .ConfigureAwait(false);

        ImmutableArray<KeyValuePair<string, string>> roomProperties = await room.GetRoomPropertiesAsync()
            .ConfigureAwait(false);

        if (roomProperties.Length > 0)
        {
            await playerPresence
                .SendComposerAsync(
                    [.. roomProperties.Select(IComposer (x) => new RoomPropertyMessageComposer
                    {
                        Key = x.Key,
                        Value = x.Value,
                    })]
                )
                .ConfigureAwait(false);
        }

        ImmutableDictionary<PlayerId, string> ownersSnapshot = await room.GetAllOwnersAsync(ct)
            .ConfigureAwait(false);
        ImmutableArray<RoomFloorItemSnapshot> floorSnapshot =
            await room.GetAllFloorItemSnapshotsAsync(ct).ConfigureAwait(false);
        ImmutableArray<RoomWallItemSnapshot> wallSnapshot = await room.GetAllWallItemSnapshotsAsync(
                ct
            )
            .ConfigureAwait(false);
        bool hasRights = controllerLevel >= RoomControllerType.Rights;
        IComposer[] danceComposers = avatarSnapshots
            .OfType<RoomPlayerAvatarSnapshot>()
            .Where(x => x.DanceType != AvatarDanceType.None)
            .Select(
                IComposer (x) =>
                    new DanceMessageComposer { ObjectId = x.ObjectId, DanceType = x.DanceType }
            )
            .ToArray();

        await playerPresence
            .SendComposerAsync(
                new RoomEntryInfoMessageComposer { RoomId = roomId, IsOwner = isOwner },
                new ObjectsMessageComposer
                {
                    OwnerNames = ownersSnapshot,
                    FloorItems = floorSnapshot,
                },
                new ItemsMessageComposer { OwnerNames = ownersSnapshot, WallItems = wallSnapshot },
                new UsersMessageComposer { Avatars = avatarSnapshots },
                new UserUpdateMessageComposer { Avatars = avatarSnapshots },
                new YouAreControllerMessageComposer
                {
                    RoomId = roomId,
                    ControllerLevel = controllerLevel,
                },
                new WiredPermissionsEventMessageComposer
                {
                    CanModify = hasRights,
                    CanRead = hasRights,
                }
            )
            .ConfigureAwait(false);

        if (isOwner)
        {
            await playerPresence
                .SendComposerAsync(new YouAreOwnerMessageComposer { RoomId = roomId })
                .ConfigureAwait(false);
        }

        if (danceComposers.Length > 0)
        {
            await playerPresence.SendComposerAsync(danceComposers).ConfigureAwait(false);
        }

        await playerPresence.SetActiveRoomAsync(roomId, ct).ConfigureAwait(false);
    }

    public async Task CloseRoomForPlayerAsync(PlayerId playerId, CancellationToken ct)
    {
        if (playerId <= 0)
        {
            return;
        }

        IPlayerPresenceGrain playerPresence = _grainFactory.GetPlayerPresenceGrain(playerId);

        await playerPresence.ClearActiveRoomAsync(ct).ConfigureAwait(false);

        await playerPresence
            .SendComposerAsync(new CloseConnectionMessageComposer())
            .ConfigureAwait(false);
    }

    public async Task ClickTileAsync(
        ActionContext ctx,
        int targetX,
        int targetY,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || ctx.RoomId <= 0)
        {
            return;
        }

        IRoomGrain roomGrain = _grainFactory.GetRoomGrain(ctx.RoomId);

        await roomGrain.ClickTileAsync(ctx, targetX, targetY, ct).ConfigureAwait(false);
        await roomGrain.WalkAvatarToAsync(ctx, targetX, targetY, ct).ConfigureAwait(false);
    }

    public async Task PickupItemInRoomAsync(
        ActionContext ctx,
        RoomObjectId itemId,
        CancellationToken ct,
        bool isConfirm = true
    )
    {
        if (ctx.PlayerId <= 0 || ctx.RoomId <= 0 || itemId <= 0)
        {
            return;
        }

        IRoomGrain roomGrain = _grainFactory.GetRoomGrain(ctx.RoomId);

        await roomGrain.RemoveItemByIdAsync(ctx, itemId, ct).ConfigureAwait(false);
    }

    public async Task UseItemInRoomAsync(
        ActionContext ctx,
        RoomObjectId itemId,
        CancellationToken ct,
        int param = -1
    )
    {
        if (ctx.PlayerId <= 0 || ctx.RoomId <= 0 || itemId <= 0)
        {
            return;
        }

        IRoomGrain roomGrain = _grainFactory.GetRoomGrain(ctx.RoomId);

        await roomGrain.UseItemByIdAsync(ctx, itemId, ct, param).ConfigureAwait(false);
    }

    public async Task ClickItemInRoomAsync(
        ActionContext ctx,
        RoomObjectId itemId,
        CancellationToken ct,
        int param = -1
    )
    {
        if (ctx.PlayerId <= 0 || ctx.RoomId <= 0 || itemId <= 0)
        {
            return;
        }

        IRoomGrain roomGrain = _grainFactory.GetRoomGrain(ctx.RoomId);

        await roomGrain.ClickItemByIdAsync(ctx, itemId, ct, param).ConfigureAwait(false);
    }
}
