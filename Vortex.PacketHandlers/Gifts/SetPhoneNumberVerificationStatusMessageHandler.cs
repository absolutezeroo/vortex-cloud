using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Gifts;

namespace Vortex.PacketHandlers.Gifts;

public class SetPhoneNumberVerificationStatusMessageHandler
    : IMessageHandler<SetPhoneNumberVerificationStatusMessage>
{
    public async ValueTask HandleAsync(
        SetPhoneNumberVerificationStatusMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
