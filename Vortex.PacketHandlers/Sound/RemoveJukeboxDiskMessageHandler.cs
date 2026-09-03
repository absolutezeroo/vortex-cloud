using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Orleans;
using Vortex.Protocol.Messages.Incoming.Sound;

namespace Vortex.PacketHandlers.Sound;

/// <summary>
/// Takes a disk back out of the room's jukebox. The room answers everyone with the new playlist.
/// </summary>
public class RemoveJukeboxDiskMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<RemoveJukeboxDiskMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        RemoveJukeboxDiskMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || ctx.RoomId <= 0)
        {
            return;
        }

        await _grainFactory
            .GetRoomJukebox(ctx.RoomId)
            .RemoveDiskAsync(ctx.AsActionContext(), message.Index, ct)
            .ConfigureAwait(false);
    }
}
