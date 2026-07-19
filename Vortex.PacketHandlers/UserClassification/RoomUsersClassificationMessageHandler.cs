using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Userclassification;

namespace Vortex.PacketHandlers.UserClassification;

public class RoomUsersClassificationMessageHandler : IMessageHandler<RoomUsersClassificationMessage>
{
    public async ValueTask HandleAsync(
        RoomUsersClassificationMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
