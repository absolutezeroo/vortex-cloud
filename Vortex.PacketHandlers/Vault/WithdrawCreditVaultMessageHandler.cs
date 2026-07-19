using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Vault;

namespace Vortex.PacketHandlers.Vault;

public class WithdrawCreditVaultMessageHandler : IMessageHandler<WithdrawCreditVaultMessage>
{
    public async ValueTask HandleAsync(
        WithdrawCreditVaultMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
