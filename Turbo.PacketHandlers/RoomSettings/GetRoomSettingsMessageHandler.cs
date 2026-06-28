using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.RoomSettings;
using Turbo.Primitives.Messages.Outgoing.Roomsettings;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Orleans.Snapshots.Room;
using Turbo.Primitives.Rooms.Grains;

namespace Turbo.PacketHandlers.RoomSettings;

public class GetRoomSettingsMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GetRoomSettingsMessage>
{
    public async ValueTask HandleAsync(
        GetRoomSettingsMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || message.RoomId <= 0)
        {
            return;
        }

        IRoomGrain roomGrain = grainFactory.GetRoomGrain(message.RoomId);
        RoomSnapshot? snapshot = await roomGrain
            .GetRoomSettingsAsync(ctx.PlayerId, ct)
            .ConfigureAwait(false);

        if (snapshot is null)
        {
            return;
        }

        await ctx.SendComposerAsync(
                new RoomSettingsDataEventMessageComposer { Settings = snapshot },
                ct
            )
            .ConfigureAwait(false);
    }
}
