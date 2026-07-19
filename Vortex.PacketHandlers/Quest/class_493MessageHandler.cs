using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Quest;

namespace Vortex.PacketHandlers.Quest;

public class class_493MessageHandler : IMessageHandler<class_493Message>
{
    public async ValueTask HandleAsync(
        class_493Message message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
