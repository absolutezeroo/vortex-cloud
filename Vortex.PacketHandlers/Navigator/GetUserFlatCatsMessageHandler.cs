using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Navigator;
using Vortex.Primitives.Orleans.Snapshots.Navigator;
using Vortex.Protocol.Messages.Incoming.Navigator;
using Vortex.Protocol.Messages.Outgoing.Navigator;

namespace Vortex.PacketHandlers.Navigator;

public class GetUserFlatCatsMessageHandler(INavigatorService navigatorService)
    : IMessageHandler<GetUserFlatCatsMessage>
{
    private readonly INavigatorService _navigatorService = navigatorService;

    public async ValueTask HandleAsync(
        GetUserFlatCatsMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        ImmutableArray<NavigatorFlatCategorySnapshot> categories =
            _navigatorService.GetFlatCategories();

        await ctx.SendComposerAsync(new UserFlatCatsMessageComposer { Categories = categories }, ct)
            .ConfigureAwait(false);
    }
}
