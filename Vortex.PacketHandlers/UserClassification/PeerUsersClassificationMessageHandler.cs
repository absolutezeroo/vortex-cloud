using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Userclassification;

namespace Vortex.PacketHandlers.UserClassification;

public class PeerUsersClassificationMessageHandler : IMessageHandler<PeerUsersClassificationMessage>
{
    public async ValueTask HandleAsync(
        PeerUsersClassificationMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
