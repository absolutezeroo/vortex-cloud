using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Quest;

namespace Vortex.PacketHandlers.Quest;

public class class_735MessageHandler : IMessageHandler<class_735Message>
{
    public async ValueTask HandleAsync(
        class_735Message message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
