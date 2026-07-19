using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Avatar;

namespace Vortex.PacketHandlers.Avatar;

public class ChangeUserNameMessageHandler : IMessageHandler<ChangeUserNameMessage>
{
    public async ValueTask HandleAsync(
        ChangeUserNameMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
