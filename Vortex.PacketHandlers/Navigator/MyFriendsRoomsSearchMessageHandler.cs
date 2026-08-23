using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Protocol.Messages.Incoming.Navigator;
using Vortex.Primitives.Navigator;

namespace Vortex.PacketHandlers.Navigator;

public class MyFriendsRoomsSearchMessageHandler(
    INavigatorService navigatorService,
    IGrainFactory grainFactory
) : IMessageHandler<MyFriendsRoomsSearchMessage>
{
    private const string SearchCode = NavigatorSearchCodes.FriendsRooms;

    public ValueTask HandleAsync(
        MyFriendsRoomsSearchMessage message,
        MessageContext ctx,
        CancellationToken ct
    ) =>
        NavigatorSearchHandlerHelper.SendSimpleSearchResultsAsync(
            navigatorService,
            grainFactory,
            SearchCode,
            ctx,
            ct
        );
}
