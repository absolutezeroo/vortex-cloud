using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Navigator;
using Turbo.Primitives.Messages.Outgoing.Navigator;
using Turbo.Primitives.Navigator;
using Turbo.Primitives.Orleans.Snapshots.Navigator;

namespace Turbo.PacketHandlers.Navigator;

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
        ImmutableArray<NavigatorFlatCategorySnapshot> categories = _navigatorService.GetFlatCategories();

        await ctx.SendComposerAsync(new UserFlatCatsMessageComposer { Categories = categories }, ct)
            .ConfigureAwait(false);
    }
}
