using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Navigator;
using Vortex.Primitives.Messages.Outgoing.Navigator;
using Vortex.Primitives.Navigator;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Orleans.Snapshots.Room;
using Vortex.Primitives.Server.Grains;

namespace Vortex.PacketHandlers.Navigator;

public class CanCreateRoomMessageHandler(
    INavigatorProvider navigatorProvider,
    IGrainFactory grainFactory
) : IMessageHandler<CanCreateRoomMessage>
{
    private const string MaxRoomsPerPlayerKey = "rooms.max_rooms_per_player";
    private const int DefaultMaxRoomsPerPlayer = 50;

    private readonly INavigatorProvider _navigatorProvider = navigatorProvider;
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        CanCreateRoomMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        int maxRooms = await _grainFactory
            .GetServerConfigGrain()
            .GetIntAsync(MaxRoomsPerPlayerKey, DefaultMaxRoomsPerPlayer)
            .ConfigureAwait(false);

        List<RoomInfoSnapshot> ownedRooms = await _navigatorProvider
            .GetRoomsByOwnerAsync(ctx.PlayerId, ct)
            .ConfigureAwait(false);

        int resultCode = ownedRooms.Count >= maxRooms ? 1 : 0;

        await ctx.SendComposerAsync(
                new CanCreateRoomMessageComposer { ResultCode = resultCode, RoomLimit = maxRooms },
                ct
            )
            .ConfigureAwait(false);
    }
}
