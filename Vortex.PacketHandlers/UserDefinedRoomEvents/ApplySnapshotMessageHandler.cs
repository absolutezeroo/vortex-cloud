using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Userdefinedroomevents;

namespace Vortex.PacketHandlers.UserDefinedRoomEvents;

public class ApplySnapshotMessageHandler : IMessageHandler<ApplySnapshotMessage>
{
    public async ValueTask HandleAsync(
        ApplySnapshotMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
