using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Protocol.Messages.Incoming.Users;

namespace Vortex.PacketHandlers.Users;

public class GetUserNftChatStylesMessageHandler : IMessageHandler<GetUserNftChatStylesMessage>
{
    public async ValueTask HandleAsync(
        GetUserNftChatStylesMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
