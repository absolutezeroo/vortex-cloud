using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Navigator;
using Vortex.Primitives.Navigator;

namespace Vortex.PacketHandlers.Navigator;

public class RoomAdSearchMessageHandler(
    INavigatorService navigatorService,
    IGrainFactory grainFactory
) : IMessageHandler<RoomAdSearchMessage>
{
    private const string SearchCode = NavigatorSearchCodes.RoomAds;

    public ValueTask HandleAsync(
        RoomAdSearchMessage message,
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
