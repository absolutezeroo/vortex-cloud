using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Campaign;

namespace Vortex.PacketHandlers.Campaign;

public class OpenCampaignCalendarDoorAsStaffMessageHandler
    : IMessageHandler<OpenCampaignCalendarDoorAsStaffMessage>
{
    public async ValueTask HandleAsync(
        OpenCampaignCalendarDoorAsStaffMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
