using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Orleans;
using Vortex.Protocol.Messages.Incoming.Room.Engine;

namespace Vortex.PacketHandlers.Room.Engine;

/// <summary>
/// Writes a furni's named fields -- the generic editor behind the furniture whose dialog is a set of
/// keys rather than a single note.
/// </summary>
public class SetObjectDataMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<SetObjectDataMessage>
{
    public async ValueTask HandleAsync(
        SetObjectDataMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || ctx.RoomId <= 0 || message.Pairs.IsDefaultOrEmpty)
        {
            return;
        }

        await grainFactory
            .GetRoomFurni(ctx.RoomId)
            .SetObjectDataAsync(ctx.AsActionContext(), message.ItemId, message.Pairs, ct)
            .ConfigureAwait(false);
    }
}
