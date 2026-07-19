using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Nux;

namespace Vortex.PacketHandlers.Nux;

public class NewUserExperienceScriptProceedMessageHandler
    : IMessageHandler<NewUserExperienceScriptProceedMessage>
{
    public async ValueTask HandleAsync(
        NewUserExperienceScriptProceedMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
