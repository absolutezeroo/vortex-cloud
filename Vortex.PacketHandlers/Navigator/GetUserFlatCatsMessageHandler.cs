using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Navigator;
using Vortex.Primitives.Messages.Outgoing.Navigator;
using Vortex.Primitives.Navigator;
using Vortex.Primitives.Orleans.Snapshots.Navigator;

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
