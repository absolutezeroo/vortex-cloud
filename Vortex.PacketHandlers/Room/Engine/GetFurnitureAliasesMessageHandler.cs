using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Room.Engine;
using Vortex.Primitives.Messages.Outgoing.Room.Engine;

namespace Vortex.PacketHandlers.Room.Engine;

public class GetFurnitureAliasesMessageHandler : IMessageHandler<GetFurnitureAliasesMessage>
{
    public async ValueTask HandleAsync(
        GetFurnitureAliasesMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ctx.SendComposerAsync(new FurnitureAliasesMessageComposer { Aliases = [] }, ct)
            .ConfigureAwait(false);
    }
}
