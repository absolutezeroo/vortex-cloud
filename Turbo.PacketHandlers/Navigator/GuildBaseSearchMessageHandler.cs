using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Navigator;
using Turbo.Primitives.Navigator;

namespace Turbo.PacketHandlers.Navigator;

public class GuildBaseSearchMessageHandler(
    INavigatorService navigatorService,
    IGrainFactory grainFactory
) : IMessageHandler<GuildBaseSearchMessage>
{
    private const string SearchCode = "groups";

    public ValueTask HandleAsync(
        GuildBaseSearchMessage message,
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
