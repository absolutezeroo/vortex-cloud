using System.Threading;
using System.Threading.Tasks;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Catalog;
using Turbo.Primitives.Messages.Outgoing.Catalog;
using Turbo.Primitives.Players;

namespace Turbo.PacketHandlers.Catalog;

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
