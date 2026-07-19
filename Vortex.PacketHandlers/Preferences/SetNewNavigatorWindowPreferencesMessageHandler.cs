using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Preferences;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.Preferences;

public class SetNewNavigatorWindowPreferencesMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<SetNewNavigatorWindowPreferencesMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        SetNewNavigatorWindowPreferencesMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await _grainFactory
            .GetPlayerNavigatorGrain(ctx.PlayerId)
            .UpdateWindowPreferencesAsync(
                message.X,
                message.Y,
                message.Width,
                message.Height,
                message.OpenSavedSearches,
                (int)message.ResultsMode,
                ct
            )
            .ConfigureAwait(false);
    }
}
