using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Nux;

namespace Vortex.PacketHandlers.Nux;

public class NewUserExperienceGetGiftsMessageHandler
    : IMessageHandler<NewUserExperienceGetGiftsMessage>
{
    public async ValueTask HandleAsync(
        NewUserExperienceGetGiftsMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
