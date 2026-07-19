using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Navigator;
using Vortex.Primitives.Messages.Outgoing.NewNavigator;
using Vortex.Primitives.Navigator;
using Vortex.Primitives.Navigator.Enums;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Orleans.Snapshots.Navigator;

namespace Vortex.PacketHandlers.Navigator;

public class MyFavouriteRoomsSearchMessageHandler(
    INavigatorService navigatorService,
    IGrainFactory grainFactory
) : IMessageHandler<MyFavouriteRoomsSearchMessage>
{
    private const string SearchCode = "myf";

    private readonly INavigatorService _navigatorService = navigatorService;
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        MyFavouriteRoomsSearchMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        ImmutableArray<NavigatorSearchResultSnapshot> results = await _navigatorService
            .GetSearchResultsAsync(
                SearchCode,
                NavigatorSearchFilterType.Anything,
                string.Empty,
                ctx.PlayerId,
                ct
            )
            .ConfigureAwait(false);

        int viewMode = await _grainFactory
            .GetPlayerNavigatorGrain(ctx.PlayerId)
            .GetViewModeAsync(SearchCode, ct)
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(
                new NavigatorSearchResultBlocksMessageComposer
                {
                    SearchCodeOriginal = SearchCode,
                    FilteringData = string.Empty,
                    Blocks =
                    [
                        new NavigatorSearchResultBlockSnapshot
                        {
                            SearchCode = SearchCode,
                            Text = string.Empty,
                            ActionAllowed = NavigatorActionAllowedType.Back,
                            Localization = string.Empty,
                            ForceClosed = false,
                            ViewMode = (NavigatorViewModeType)viewMode,
                            Results = results,
                        },
                    ],
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
