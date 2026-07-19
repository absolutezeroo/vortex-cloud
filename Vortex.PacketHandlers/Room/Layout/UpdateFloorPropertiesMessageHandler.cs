using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Room.Layout;

namespace Vortex.PacketHandlers.Room.Layout;

public class UpdateFloorPropertiesMessageHandler : IMessageHandler<UpdateFloorPropertiesMessage>
{
    public async ValueTask HandleAsync(
        UpdateFloorPropertiesMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
