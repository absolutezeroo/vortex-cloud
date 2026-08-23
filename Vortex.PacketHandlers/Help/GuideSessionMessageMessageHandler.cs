using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Help.Grains;
using Vortex.Primitives.Orleans;
using Vortex.Protocol.Messages.Incoming.Help;
using Vortex.Protocol.Messages.Outgoing.Help;

namespace Vortex.PacketHandlers.Help;

/// <summary>
/// A line of chat inside a guide session, echoed to both sides.
/// </summary>
/// <remarks>
/// Back to the sender as well as on to the partner: the client does not draw its own line locally,
/// it waits for the server to send it back. Skipping the echo leaves the sender typing into a
/// window where nothing they say ever appears.
/// </remarks>
public class GuideSessionMessageMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GuideSessionMessageMessage>
{
    public async ValueTask HandleAsync(
        GuideSessionMessageMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || string.IsNullOrEmpty(message.Message))
        {
            return;
        }

        int partnerId = await grainFactory
            .GetGuideDirectoryGrain()
            .GetPartnerAsync(ctx.PlayerId, ct)
            .ConfigureAwait(false);

        // No session, no chat. The packet carries no destination, so the session is the only thing
        // saying where this line may go -- without the check it would be a message to nobody, or to
        // whoever happened to be paired last.
        if (partnerId <= 0)
        {
            return;
        }

        GuideSessionMessageMessageComposer echo = new()
        {
            ChatMessage = message.Message,
            SenderId = ctx.PlayerId,
        };

        await ctx.SendComposerAsync(echo, ct).ConfigureAwait(false);

        await grainFactory
            .GetPlayerPresenceGrain(partnerId)
            .SendComposerAsync(echo)
            .ConfigureAwait(false);
    }
}
