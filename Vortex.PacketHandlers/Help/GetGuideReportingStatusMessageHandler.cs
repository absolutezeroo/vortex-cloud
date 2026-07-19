using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Help;

namespace Vortex.PacketHandlers.Help;

public class GetGuideReportingStatusMessageHandler : IMessageHandler<GetGuideReportingStatusMessage>
{
    public async ValueTask HandleAsync(
        GetGuideReportingStatusMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
