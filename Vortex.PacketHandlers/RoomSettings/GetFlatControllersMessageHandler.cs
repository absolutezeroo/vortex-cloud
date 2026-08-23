using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Protocol.Messages.Incoming.RoomSettings;
using Vortex.Protocol.Messages.Outgoing.Roomsettings;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Orleans.Snapshots.Room;
using Vortex.Primitives.Rooms.Grains;

namespace Vortex.PacketHandlers.RoomSettings;

public class GetFlatControllersMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GetFlatControllersMessage>
{
    public async ValueTask HandleAsync(
        GetFlatControllersMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || message.RoomId <= 0)
        {
            return;
        }

        IRoomSettings roomGrain = grainFactory.GetRoomSettings(message.RoomId);
        ImmutableArray<RoomControllerSnapshot> controllers = await roomGrain
            .GetControllersAsync(ct)
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(
                new FlatControllersEventMessageComposer
                {
                    RoomId = message.RoomId,
                    Controllers = controllers,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
