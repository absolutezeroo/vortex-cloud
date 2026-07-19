using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Camera;

namespace Vortex.PacketHandlers.Camera;

public class PhotoCompetitionMessageHandler : IMessageHandler<PhotoCompetitionMessage>
{
    public async ValueTask HandleAsync(
        PhotoCompetitionMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
