using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.NewNavigator;
using Turbo.Primitives.Orleans;

namespace Turbo.PacketHandlers.NewNavigator;

public class NavigatorAddCollapsedCategoryMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<NavigatorAddCollapsedCategoryMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        NavigatorAddCollapsedCategoryMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (string.IsNullOrWhiteSpace(message.CategoryName))
            return;

        await _grainFactory
            .GetPlayerNavigatorGrain(ctx.PlayerId)
            .AddCollapsedCategoryAsync(message.CategoryName, ct)
            .ConfigureAwait(false);
    }
}
