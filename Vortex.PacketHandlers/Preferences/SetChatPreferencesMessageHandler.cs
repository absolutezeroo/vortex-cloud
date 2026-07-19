using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Preferences;

namespace Vortex.PacketHandlers.Preferences;

public class SetChatPreferencesMessageHandler : IMessageHandler<SetChatPreferencesMessage>
{
    public async ValueTask HandleAsync(
        SetChatPreferencesMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
