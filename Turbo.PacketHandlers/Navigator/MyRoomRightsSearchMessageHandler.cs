using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Navigator;
using Turbo.Primitives.Navigator;

namespace Turbo.PacketHandlers.Navigator;

public class MyRoomRightsSearchMessageHandler(
    INavigatorService navigatorService,
    IGrainFactory grainFactory
) : IMessageHandler<MyRoomRightsSearchMessage>
{
    private const string SearchCode = "with_rights";

    public ValueTask HandleAsync(
        MyRoomRightsSearchMessage message,
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
