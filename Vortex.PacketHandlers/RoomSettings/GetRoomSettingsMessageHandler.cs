using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.RoomSettings;
using Vortex.Primitives.Messages.Outgoing.Roomsettings;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Orleans.Snapshots.Room;
using Vortex.Primitives.Rooms.Grains;

namespace Vortex.PacketHandlers.RoomSettings;

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
