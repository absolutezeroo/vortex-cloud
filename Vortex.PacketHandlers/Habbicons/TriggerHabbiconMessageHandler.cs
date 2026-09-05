using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Orleans;
using Vortex.Protocol.Messages.Incoming.Habbicons;

namespace Vortex.PacketHandlers.Habbicons;

/// <summary>
/// Use a Habbicon in the room the session is in. The room id comes from the session, not the
/// packet: the client sends nothing but a Habbicon id.
/// </summary>
public class TriggerHabbiconMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<TriggerHabbiconMessage>
{
    public async ValueTask HandleAsync(
        TriggerHabbiconMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || ctx.RoomId <= 0 || message.HabbiconId <= 0)
        {
            return;
        }

        await grainFactory
            .GetPlayerHabbiconGrain(ctx.PlayerId)
            .UseInRoomAsync(ctx.RoomId, message.HabbiconId, ct)
            .ConfigureAwait(false);
    }
}
