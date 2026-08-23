using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Help.Grains;
using Vortex.Protocol.Messages.Incoming.Help;
using Vortex.Protocol.Messages.Outgoing.Help;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.Help;

/// <summary>
/// Passes the typing indicator to the other side only. Unlike the chat there is no echo: the sender
/// knows perfectly well that they are typing.
/// </summary>
public class GuideSessionIsTypingMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GuideSessionIsTypingMessage>
{
    public async ValueTask HandleAsync(
        GuideSessionIsTypingMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        int partnerId = await grainFactory
            .GetGuideDirectoryGrain()
            .GetPartnerAsync(ctx.PlayerId, ct)
            .ConfigureAwait(false);

        if (partnerId <= 0)
        {
            return;
        }

        await grainFactory
            .GetPlayerPresenceGrain(partnerId)
            .SendComposerAsync(
                new GuideSessionPartnerIsTypingMessageComposer { IsTyping = message.IsTyping }
            )
            .ConfigureAwait(false);
    }
}
