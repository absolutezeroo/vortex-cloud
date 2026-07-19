using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Vault;

namespace Vortex.PacketHandlers.Vault;

public class CreditVaultStatusMessageHandler : IMessageHandler<CreditVaultStatusMessage>
{
    public async ValueTask HandleAsync(
        CreditVaultStatusMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
