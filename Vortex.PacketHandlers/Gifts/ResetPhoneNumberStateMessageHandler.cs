using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Gifts;

namespace Vortex.PacketHandlers.Gifts;

public class ResetPhoneNumberStateMessageHandler : IMessageHandler<ResetPhoneNumberStateMessage>
{
    public async ValueTask HandleAsync(
        ResetPhoneNumberStateMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
