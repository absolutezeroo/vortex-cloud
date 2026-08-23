using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Preferences.Enums;
using Vortex.Protocol.Messages.Incoming.Preferences;
using Vortex.Protocol.Messages.Outgoing.Preferences;

namespace Vortex.PacketHandlers.Preferences;

public class AddToCustomFilterMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<AddToCustomFilterMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    /// <summary>
    /// The reply is always sent, refusal included: the client applies nothing locally, so staying
    /// silent would leave its input cleared and the word nowhere.
    /// </summary>
    public async ValueTask HandleAsync(
        AddToCustomFilterMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        bool added = await _grainFactory
            .GetPlayerGrain(ctx.PlayerId)
            .AddWordFilterAsync(message.Word, ct)
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(
                new ModifyCustomFilterResultMessageComposer
                {
                    Result = added
                        ? WordFilterModifyResultType.Added
                        : WordFilterModifyResultType.Failed,
                    Word = message.Word,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
