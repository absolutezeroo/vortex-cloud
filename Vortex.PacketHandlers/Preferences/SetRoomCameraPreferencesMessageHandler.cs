using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Preferences;

namespace Vortex.PacketHandlers.Preferences;

public class SetRoomCameraPreferencesMessageHandler
    : IMessageHandler<SetRoomCameraPreferencesMessage>
{
    public async ValueTask HandleAsync(
        SetRoomCameraPreferencesMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
