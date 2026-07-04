using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Navigator;
using Turbo.Primitives.Navigator;

namespace Turbo.PacketHandlers.Navigator;

public class MyFriendsRoomsSearchMessageHandler(
    INavigatorService navigatorService,
    IGrainFactory grainFactory
) : IMessageHandler<MyFriendsRoomsSearchMessage>
{
    private const string SearchCode = "friends_rooms";

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
