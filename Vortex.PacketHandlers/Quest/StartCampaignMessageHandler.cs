using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Quest;

namespace Vortex.PacketHandlers.Quest;

public class StartCampaignMessageHandler : IMessageHandler<StartCampaignMessage>
{
    public async ValueTask HandleAsync(
        StartCampaignMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
