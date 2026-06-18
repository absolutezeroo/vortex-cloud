using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Turbo.Database.Context;
using Turbo.Primitives.Action;
using Turbo.Primitives.Messages.Outgoing.Navigator;
using Turbo.Primitives.Messages.Outgoing.Room.Engine;
using Turbo.Primitives.Messages.Outgoing.Room.Layout;
using Turbo.Primitives.Messages.Outgoing.Room.Session;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Players;
using Turbo.Primitives.Rooms;
using Turbo.Primitives.Rooms.Enums;
using Turbo.Primitives.Rooms.Object;
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
    private readonly ILogger<IRoomService> _logger = logger;
    private readonly RoomConfig _roomConfig = roomConfig.Value;
    private readonly ISessionGateway _sessionGateway = sessionGateway;
    private readonly IGrainFactory _grainFactory = grainFactory;
    private readonly IDbContextFactory<TurboDbContext> _dbContextFactory = dbContextFactory;
    private readonly IRoomModerationStore _roomModerationStore = roomModerationStore;

    public async Task OpenRoomForPlayerIdAsync(
        ActionContext ctx,
        PlayerId playerId,
        RoomId roomId,
        CancellationToken ct
    )
    {
        try
        {
            var playerPresence = _grainFactory.GetPlayerPresenceGrain(playerId);
            var pendingRoom = await playerPresence.GetPendingRoomAsync().ConfigureAwait(false);

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
                return;

            await playerPresence.ClearActiveRoomAsync(ct).ConfigureAwait(false);
            await playerPresence.SetPendingRoomAsync(roomId, true).ConfigureAwait(false);

            // if owner => auto-approve
            // if banned => reject
            // if full => reject
            // if passworded => reject (for now)
            // if locked => reject (for now)

            var room = _grainFactory.GetRoomGrain(roomId);

            await playerPresence
                .SendComposerAsync(new OpenConnectionMessageComposer { RoomId = roomId })
                .ConfigureAwait(false);

            await room.EnsureRoomActiveAsync(ct).ConfigureAwait(false);

            var snapshot = await room.GetSnapshotAsync().ConfigureAwait(false);
            var mapSnapshot = await room.GetMapSnapshotAsync(ct).ConfigureAwait(false);

            await playerPresence
                .SendComposerAsync(
                    new RoomReadyMessageComposer
                    {
                        WorldType = snapshot.WorldType,
                        RoomId = roomId,
                    },
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
                    }
                )
                .ConfigureAwait(false);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task CloseRoomForPlayerAsync(PlayerId playerId, CancellationToken ct)
    {
        if (playerId <= 0)
            return;

        var playerPresence = _grainFactory.GetPlayerPresenceGrain(playerId);

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
            return;

        var roomGrain = _grainFactory.GetRoomGrain(ctx.RoomId);

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
            return;

        var roomGrain = _grainFactory.GetRoomGrain(ctx.RoomId);

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
            return;

        var roomGrain = _grainFactory.GetRoomGrain(ctx.RoomId);

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
            return;

        var roomGrain = _grainFactory.GetRoomGrain(ctx.RoomId);

        await roomGrain.ClickItemByIdAsync(ctx, itemId, ct, param).ConfigureAwait(false);
    }
}
