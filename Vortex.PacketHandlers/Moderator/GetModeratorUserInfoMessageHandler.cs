using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Moderator;

namespace Vortex.PacketHandlers.Moderator;

public class GetModeratorUserInfoMessageHandler : IMessageHandler<GetModeratorUserInfoMessage>
{
    public async ValueTask HandleAsync(
        GetModeratorUserInfoMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
