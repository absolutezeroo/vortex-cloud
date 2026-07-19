using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Catalog;
using Vortex.Primitives.Messages.Outgoing.Catalog;
using Vortex.Primitives.Players;

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
