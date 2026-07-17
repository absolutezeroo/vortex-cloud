using System.Threading;
using System.Threading.Tasks;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Inventory.Trading;

namespace Turbo.PacketHandlers.Inventory.Trading;

public class SilverFeeMessageHandler : IMessageHandler<SilverFeeMessage>
{
    public async ValueTask HandleAsync(
        SilverFeeMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        // Adding silver/diamond currency to a trade is a newer WIN63 feature with no wire format in
        // the available client sources; the core furniture-exchange loop does not depend on it. Left
        // as an intentional no-op until the silver-fee protocol can be verified against a real client.
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
