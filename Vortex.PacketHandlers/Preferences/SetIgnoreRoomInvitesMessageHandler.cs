using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Preferences;

namespace Vortex.PacketHandlers.Preferences;

public class SetIgnoreRoomInvitesMessageHandler : IMessageHandler<SetIgnoreRoomInvitesMessage>
{
    public async ValueTask HandleAsync(
        SetIgnoreRoomInvitesMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
