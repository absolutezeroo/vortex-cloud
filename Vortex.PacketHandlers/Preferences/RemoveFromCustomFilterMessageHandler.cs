using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Preferences;
using Vortex.Primitives.Messages.Outgoing.Preferences;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Preferences.Enums;

namespace Vortex.PacketHandlers.Preferences;

public class RemoveFromCustomFilterMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<RemoveFromCustomFilterMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        RemoveFromCustomFilterMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        bool removed = await _grainFactory
            .GetPlayerGrain(ctx.PlayerId)
            .RemoveWordFilterAsync(message.Word, ct)
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(
                new ModifyCustomFilterResultMessageComposer
                {
                    Result = removed
                        ? WordFilterModifyResultType.Removed
                        : WordFilterModifyResultType.Failed,
                    Word = message.Word,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
