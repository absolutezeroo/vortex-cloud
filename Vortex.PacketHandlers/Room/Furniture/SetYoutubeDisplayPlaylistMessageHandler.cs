using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Room.Furniture;

namespace Vortex.PacketHandlers.Room.Furniture;

public class SetYoutubeDisplayPlaylistMessageHandler
    : IMessageHandler<SetYoutubeDisplayPlaylistMessage>
{
    public async ValueTask HandleAsync(
        SetYoutubeDisplayPlaylistMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
