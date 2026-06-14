using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.NewNavigator;
using Turbo.Primitives.Orleans;

namespace Turbo.PacketHandlers.NewNavigator;

public class NavigatorAddSavedSearchMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<NavigatorAddSavedSearchMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        NavigatorAddSavedSearchMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (string.IsNullOrWhiteSpace(message.SearchCode))
            return;

        await _grainFactory
            .GetPlayerNavigatorGrain(ctx.PlayerId)
            .AddSavedSearchAsync(message.SearchCode, message.Filter ?? string.Empty, ct)
            .ConfigureAwait(false);
    }
}
