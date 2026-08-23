using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Rooms;
using Vortex.Primitives.Rooms.Grains;
using Vortex.Protocol.Messages.Incoming.RoomSettings;

namespace Vortex.PacketHandlers.RoomSettings;

public class SaveRoomSettingsMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<SaveRoomSettingsMessage>
{
    public async ValueTask HandleAsync(
        SaveRoomSettingsMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || message.RoomId <= 0)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(message.RoomName))
        {
            return;
        }

        IRoomSettings roomGrain = grainFactory.GetRoomSettings(message.RoomId);

        await roomGrain
            .UpdateRoomSettingsAsync(
                ctx.PlayerId,
                new RoomSettingsUpdate
                {
                    Name = message.RoomName,
                    Description = message.RoomDescription,
                    DoorMode = (Vortex.Primitives.Rooms.Enums.RoomDoorModeType)message.DoorMode,
                    Password = message.Password,
                    MaxVisitors = message.MaxVisitors,
                    CategoryId = message.CategoryId,
                    TradeMode = message.TradeMode,
                    AllowPets = message.AllowPets,
                    AllowPetsEat = message.AllowFoodConsume,
                    AllowBlocking = message.AllowWalkThrough,
                    HideWalls = message.HideWalls,
                    WallThickness = message.WallThickness,
                    FloorThickness = message.FloorThickness,
                    WhoCanMute = message.WhoCanMute,
                    WhoCanKick = message.WhoCanKick,
                    WhoCanBan = message.WhoCanBan,
                    ChatFloodSensitivity = message.ChatFloodSensitivity,
                    LeaveOnDoorTile = message.LeaveOnDoorTileEnabled,
                    IdleSleepEnabled = message.IdleSleepEnabled,
                    IdleSleepTimeoutSeconds = message.IdleSleepTimeoutSeconds,
                    IdleAutokickEnabled = message.IdleAutokickEnabled,
                    IdleAutokickTimeoutSeconds = message.IdleAutokickTimeoutSeconds,
                    MuteAllPets = message.MuteAllPets,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
