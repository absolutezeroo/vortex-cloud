using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Catalog.Grains;
using Turbo.Primitives.Messages.Incoming.Catalog;
using Turbo.Primitives.Orleans;

namespace Turbo.PacketHandlers.Catalog;

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
