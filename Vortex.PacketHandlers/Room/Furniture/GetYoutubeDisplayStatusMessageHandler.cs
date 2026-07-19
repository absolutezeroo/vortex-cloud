using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Room.Furniture;

namespace Vortex.PacketHandlers.Room.Furniture;

public class GetYoutubeDisplayStatusMessageHandler : IMessageHandler<GetYoutubeDisplayStatusMessage>
{
    public async ValueTask HandleAsync(
        GetYoutubeDisplayStatusMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
