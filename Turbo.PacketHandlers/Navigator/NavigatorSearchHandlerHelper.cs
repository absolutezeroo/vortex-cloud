using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Outgoing.NewNavigator;
using Turbo.Primitives.Navigator;
using Turbo.Primitives.Navigator.Enums;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Orleans.Snapshots.Navigator;

namespace Turbo.PacketHandlers.Navigator;

/// <summary>
///     Shared orchestration for the navigator search handlers that just resolve a fixed
///     search code to results and send back a single-block response (guilds, room ads,
///     official rooms, friends' rooms, rooms with rights).
/// </summary>
internal static class NavigatorSearchHandlerHelper
{
    public static async ValueTask SendSimpleSearchResultsAsync(
        INavigatorService navigatorService,
        IGrainFactory grainFactory,
        string searchCode,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        ImmutableArray<NavigatorSearchResultSnapshot> results = await navigatorService
            .GetSearchResultsAsync(
                searchCode,
                NavigatorSearchFilterType.Anything,
                string.Empty,
                ctx.PlayerId,
                ct
            )
            .ConfigureAwait(false);

        int viewMode = await grainFactory
            .GetPlayerNavigatorGrain(ctx.PlayerId)
            .GetViewModeAsync(searchCode, ct)
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(
                new NavigatorSearchResultBlocksMessageComposer
                {
                    SearchCodeOriginal = searchCode,
                    FilteringData = string.Empty,
                    Blocks =
                    [
                        new NavigatorSearchResultBlockSnapshot
                        {
                            SearchCode = searchCode,
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
