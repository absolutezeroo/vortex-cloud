using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Catalog.Grains;
using Vortex.Primitives.Messages.Incoming.Catalog;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.Catalog;

public class RedeemVoucherMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<RedeemVoucherMessage>
{
    public async ValueTask HandleAsync(
        RedeemVoucherMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        string? code = message.Code;

        if (string.IsNullOrWhiteSpace(code))
        {
            return;
        }

        IVoucherGrain voucher = grainFactory.GetVoucherGrain(code);

        await voucher.RedeemAsync(ctx.PlayerId, ct).ConfigureAwait(false);
    }
}
