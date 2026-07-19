using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.NewNavigator;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.NewNavigator;

public class NavigatorDeleteSavedSearchMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<NavigatorDeleteSavedSearchMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        NavigatorDeleteSavedSearchMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await _grainFactory
            .GetPlayerNavigatorGrain(ctx.PlayerId)
            .DeleteSavedSearchAsync(message.SearchId, ct)
            .ConfigureAwait(false);
    }
}
