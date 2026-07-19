using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Users;

namespace Vortex.PacketHandlers.Users;

public class GiveStarGemToUserMessageHandler : IMessageHandler<GiveStarGemToUserMessage>
{
    public async ValueTask HandleAsync(
        GiveStarGemToUserMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
