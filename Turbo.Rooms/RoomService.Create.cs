using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Turbo.Database.Context;
using Turbo.Database.Entities.Players;
using Turbo.Database.Entities.Room;
using Turbo.Primitives.Navigator.Enums;
using Turbo.Primitives.Players;
using Turbo.Primitives.Rooms;
using Turbo.Primitives.Rooms.Enums;

namespace Turbo.Rooms;

internal sealed partial class RoomService
{
    public async Task<(RoomId RoomId, string Name)> CreateRoomAsync(
        string name,
        string description,
        string modelName,
        int categoryId,
        int maxPlayers,
        RoomTradeModeType tradeType,
        PlayerId playerId,
        CancellationToken ct
    )
    {
        await using TurboDbContext dbCtx = await _dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        RoomModelEntity model =
            await dbCtx
                .RoomModels.FirstOrDefaultAsync(x => x.Name == modelName && x.DeletedAt == null, ct)
                .ConfigureAwait(false)
            ?? await dbCtx
                .RoomModels.FirstOrDefaultAsync(x => x.DeletedAt == null, ct)
                .ConfigureAwait(false)
            ?? throw new InvalidOperationException("No room models available.");

        PlayerEntity player =
            await dbCtx
                .Players.FirstOrDefaultAsync(x => x.Id == playerId.Value && x.DeletedAt == null, ct)
                .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Player {playerId} not found.");

        string trimmedName = name.Trim();

        RoomEntity room = new RoomEntity
        {
            Name = trimmedName,
            Description = description.Trim(),
            PlayerEntityId = player.Id,
            PlayerEntity = player,
            RoomModelEntityId = model.Id,
            RoomModelEntity = model,
            NavigatorCategoryEntityId = categoryId > 0 ? categoryId : null,
            DoorMode = RoomDoorModeType.Open,
            UsersNow = 0,
            PlayersMax = maxPlayers,
            TradeType = tradeType,
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
            MuteType = ModSettingType.Owner,
            KickType = ModSettingType.Owner,
            BanType = ModSettingType.Owner,
            ChatModeType = ChatModeType.FreeFlow,
            ChatBubbleType = ChatBubbleWidthType.Normal,
            ChatSpeedType = ChatScrollSpeedType.Normal,
            ChatFloodType = ChatFloodSensitivityType.Minimal,
            ChatDistance = 50,
        };

        dbCtx.Rooms.Add(room);
        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Room created: RoomId={RoomId} Name={Name} Owner={PlayerId}",
            room.Id,
            trimmedName,
            playerId
        );

        return (room.Id, trimmedName);
    }
}
