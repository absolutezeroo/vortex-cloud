using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Protocol.Messages.Incoming.Campaign;

namespace Vortex.PacketHandlers.Campaign;

public class OpenCampaignCalendarDoorMessageHandler
    : IMessageHandler<OpenCampaignCalendarDoorMessage>
{
    public async ValueTask HandleAsync(
        OpenCampaignCalendarDoorMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
