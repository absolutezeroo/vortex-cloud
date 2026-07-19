using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Vault;

namespace Docs.Patterns;

// Reference-only sample: mirrors packet handler shape used in Vortex.PacketHandlers.
public sealed class HandlerPattern : IMessageHandler<CreditVaultStatusMessage>
{
    public async ValueTask HandleAsync(
        CreditVaultStatusMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        ct.ThrowIfCancellationRequested();

        if (message is null)
        {
            return;
        }

        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
