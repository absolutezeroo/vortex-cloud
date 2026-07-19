using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Gifts;

namespace Vortex.PacketHandlers.Gifts;

public class class_200MessageHandler : IMessageHandler<class_200Message>
{
    public async ValueTask HandleAsync(
        class_200Message message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
