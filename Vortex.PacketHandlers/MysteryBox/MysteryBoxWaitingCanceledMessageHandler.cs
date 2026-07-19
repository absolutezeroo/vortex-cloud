using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Mysterybox;

namespace Vortex.PacketHandlers.MysteryBox;

public class MysteryBoxWaitingCanceledMessageHandler
    : IMessageHandler<MysteryBoxWaitingCanceledMessage>
{
    public async ValueTask HandleAsync(
        MysteryBoxWaitingCanceledMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
