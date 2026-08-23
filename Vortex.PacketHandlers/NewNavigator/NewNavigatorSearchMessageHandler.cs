using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Protocol.Messages.Incoming.NewNavigator;
using Vortex.Protocol.Messages.Outgoing.NewNavigator;
using Vortex.Primitives.Navigator;
using Vortex.Primitives.Navigator.Enums;
using Vortex.Primitives.Orleans.Snapshots.Navigator;

namespace Vortex.PacketHandlers.NewNavigator;

public class NewNavigatorSearchMessageHandler(INavigatorService navigatorService)
    : IMessageHandler<NewNavigatorSearchMessage>
{
    private readonly INavigatorService _navigatorService = navigatorService;

    public async ValueTask HandleAsync(
        NewNavigatorSearchMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        string searchCode = message.SearchCodeOriginal ?? string.Empty;
        string filterRaw = message.FilteringData ?? string.Empty;

        (NavigatorSearchFilterType filterType, string filterValue) = NavigatorSearchFilter.Parse(
            filterRaw
        );

        // The service decides how many blocks the answer has: a tab expands into one block per
        // quick link, everything else is a single block. This used to always wrap one flat room
        // list, so every tab rendered as a single list.
        ImmutableArray<NavigatorSearchResultBlockSnapshot> blocks = await _navigatorService
            .GetSearchBlocksAsync(searchCode, filterType, filterValue, ctx.PlayerId, ct)
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(
                new NavigatorSearchResultBlocksMessageComposer
                {
                    SearchCodeOriginal = searchCode,
                    FilteringData = NavigatorSearchFilter.Format(filterType, filterValue),
                    Blocks = blocks,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
