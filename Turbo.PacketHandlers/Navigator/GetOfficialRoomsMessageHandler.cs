using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Navigator;
using Turbo.Primitives.Navigator;

namespace Turbo.PacketHandlers.Navigator;

public class GetOfficialRoomsMessageHandler(
    INavigatorService navigatorService,
    IGrainFactory grainFactory
) : IMessageHandler<GetOfficialRoomsMessage>
{
    private const string SearchCode = "official";

    public ValueTask HandleAsync(
        GetOfficialRoomsMessage message,
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
