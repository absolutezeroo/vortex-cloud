using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Talent;

namespace Vortex.PacketHandlers.Talent;

public class GetTalentTrackMessageHandler : IMessageHandler<GetTalentTrackMessage>
{
    public async ValueTask HandleAsync(
        GetTalentTrackMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
