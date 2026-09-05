using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Orleans;
using Vortex.Protocol.Messages.Incoming.Habbicons;

namespace Vortex.PacketHandlers.Habbicons;

/// <summary>
/// A Habbicon sent into a private conversation. It goes down the messenger's ordinary send path, so
/// the friend and block rules that apply to a line of text apply to this too.
/// </summary>
public class SendHabbiconMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<SendHabbiconMessage>
{
    public async ValueTask HandleAsync(
        SendHabbiconMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || message.ChatId <= 0 || message.HabbiconId <= 0)
        {
            return;
        }

        await grainFactory
            .GetPlayerHabbiconGrain(ctx.PlayerId)
            .UseInConversationAsync(message.ChatId, message.HabbiconId, message.ConfirmationId, ct)
            .ConfigureAwait(false);
    }
}
