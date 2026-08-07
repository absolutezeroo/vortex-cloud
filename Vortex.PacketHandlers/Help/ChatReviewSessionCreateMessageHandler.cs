using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Help.Grains;
using Vortex.Primitives.Messages.Incoming.Help;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.Help;

/// <summary>
/// A player reporting a conversation for the guardians to judge.
/// </summary>
public class ChatReviewSessionCreateMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<ChatReviewSessionCreateMessage>
{
    public async ValueTask HandleAsync(
        ChatReviewSessionCreateMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || string.IsNullOrWhiteSpace(message.Message))
        {
            return;
        }

        await grainFactory
            .GetGuideDirectoryGrain()
            .CreateChatReviewAsync(ctx.PlayerId, message.Message, ct)
            .ConfigureAwait(false);
    }
}
