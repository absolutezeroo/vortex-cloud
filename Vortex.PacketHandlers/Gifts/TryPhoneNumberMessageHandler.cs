using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Gifts;

namespace Vortex.PacketHandlers.Gifts;

public class TryPhoneNumberMessageHandler : IMessageHandler<TryPhoneNumberMessage>
{
    public async ValueTask HandleAsync(
        TryPhoneNumberMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
