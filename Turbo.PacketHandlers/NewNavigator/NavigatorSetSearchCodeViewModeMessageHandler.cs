using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.NewNavigator;
using Turbo.Primitives.Orleans;

namespace Turbo.PacketHandlers.NewNavigator;

public class NavigatorSetSearchCodeViewModeMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<NavigatorSetSearchCodeViewModeMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        NavigatorSetSearchCodeViewModeMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (string.IsNullOrWhiteSpace(message.CategoryName))
        {
            return;
        }

        await _grainFactory
            .GetPlayerNavigatorGrain(ctx.PlayerId)
            .SetViewModeAsync(message.CategoryName, (int)message.ViewMode, ct)
            .ConfigureAwait(false);
    }
}
