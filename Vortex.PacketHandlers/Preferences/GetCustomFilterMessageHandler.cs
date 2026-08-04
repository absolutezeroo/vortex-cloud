using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Preferences;
using Vortex.Primitives.Messages.Outgoing.Preferences;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.Preferences;

public class GetCustomFilterMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GetCustomFilterMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        GetCustomFilterMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        ImmutableArray<string> words = await _grainFactory
            .GetPlayerGrain(ctx.PlayerId)
            .GetWordFilterAsync(ct)
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(new GetCustomFilterResultMessageComposer { Words = words }, ct)
            .ConfigureAwait(false);
    }
}
