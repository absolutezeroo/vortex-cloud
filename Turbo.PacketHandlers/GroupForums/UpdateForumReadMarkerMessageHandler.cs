using System.Threading;
using System.Threading.Tasks;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.GroupForums;

namespace Turbo.PacketHandlers.GroupForums;

public class UpdateForumReadMarkerMessageHandler : IMessageHandler<UpdateForumReadMarkerMessage>
{
    public async ValueTask HandleAsync(
        UpdateForumReadMarkerMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
