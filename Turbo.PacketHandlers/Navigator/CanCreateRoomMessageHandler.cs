using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Navigator;
using Turbo.Primitives.Messages.Outgoing.Navigator;
using Turbo.Primitives.Navigator;
using Turbo.Primitives.Orleans.Snapshots.Room;

namespace Turbo.PacketHandlers.Navigator;

public class CanCreateRoomMessageHandler(
    INavigatorProvider navigatorProvider,
    IConfiguration configuration
) : IMessageHandler<CanCreateRoomMessage>
{
    private const int DefaultMaxRoomsPerPlayer = 50;

    private readonly INavigatorProvider _navigatorProvider = navigatorProvider;
    private readonly IConfiguration _configuration = configuration;

    public async ValueTask HandleAsync(
        CanCreateRoomMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        int maxRooms = _configuration.GetValue(
            "Turbo:Rooms:MaxRoomsPerPlayer",
            DefaultMaxRoomsPerPlayer
        );

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
