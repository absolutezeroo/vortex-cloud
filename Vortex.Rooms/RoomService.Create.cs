using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vortex.Database.Context;
using Vortex.Database.Entities.Players;
using Vortex.Database.Entities.Room;
using Vortex.Primitives.Navigator.Enums;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.Rooms;

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
        await using VortexDbContext dbCtx = await _dbContextFactory
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
            AllowPets = true,
            AllowPetsEat = false,
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
