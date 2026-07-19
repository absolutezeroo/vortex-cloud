using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Talent;

namespace Vortex.PacketHandlers.Talent;

public class GetTalentTrackLevelMessageHandler : IMessageHandler<GetTalentTrackLevelMessage>
{
    public async ValueTask HandleAsync(
        GetTalentTrackLevelMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
