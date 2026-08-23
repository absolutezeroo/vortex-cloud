using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Protocol.Messages.Incoming.Room.Furniture;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.Room.Furniture;

/// <summary>
/// Cashes a credit furni in. The room consumes the coin and reports what it was worth; the wallet
/// pays it out and pushes the new balance, which is what the client redraws.
/// </summary>
public class CreditFurniRedeemMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<CreditFurniRedeemMessage>
{
    public async ValueTask HandleAsync(
        CreditFurniRedeemMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || ctx.RoomId <= 0)
        {
            return;
        }

        int credits = await grainFactory
            .GetRoomFurni(ctx.RoomId)
            .RedeemCreditFurniAsync(ctx.AsActionContext(), message.ObjectId, ct)
            .ConfigureAwait(false);

        if (credits <= 0)
        {
            return;
        }

        await grainFactory
            .GetPlayerWalletGrain(ctx.PlayerId)
            .GrantCreditsAsync(credits, ct)
            .ConfigureAwait(false);
    }
}
