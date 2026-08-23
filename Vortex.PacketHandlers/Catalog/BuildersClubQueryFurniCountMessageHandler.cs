using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Players;
using Vortex.Protocol.Messages.Incoming.Catalog;
using Vortex.Protocol.Messages.Outgoing.Catalog;

namespace Vortex.PacketHandlers.Catalog;

public class BuildersClubQueryFurniCountMessageHandler(IBuildersClubService buildersClubService)
    : IMessageHandler<BuildersClubQueryFurniCountMessage>
{
    public async ValueTask HandleAsync(
        BuildersClubQueryFurniCountMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        int furniCount = await buildersClubService
            .GetOwnedFurnitureCountAsync(ctx.PlayerId, ct)
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(
                new BuildersClubFurniCountMessageComposer { FurniCount = furniCount },
                ct
            )
            .ConfigureAwait(false);
    }
}
