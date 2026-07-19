using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Users;

namespace Vortex.PacketHandlers.Users;

public class UnblockGroupMemberMessageHandler : IMessageHandler<UnblockGroupMemberMessage>
{
    public async ValueTask HandleAsync(
        UnblockGroupMemberMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
