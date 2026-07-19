using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.NewNavigator;
using Vortex.Primitives.Messages.Outgoing.NewNavigator;
using Vortex.Primitives.Navigator;
using Vortex.Primitives.Navigator.Enums;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Orleans.Snapshots.Navigator;

namespace Vortex.PacketHandlers.NewNavigator;

public class NewNavigatorSearchMessageHandler(
    INavigatorService navigatorService,
    IGrainFactory grainFactory
) : IMessageHandler<NewNavigatorSearchMessage>
{
    private readonly INavigatorService _navigatorService = navigatorService;
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        NewNavigatorSearchMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        string searchCode = message.SearchCodeOriginal ?? string.Empty;
        string filterRaw = message.FilteringData ?? string.Empty;

        NavigatorSearchFilterType filterType = NavigatorSearchFilterType.Anything;
        string filterValue = string.Empty;

        if (!string.IsNullOrWhiteSpace(filterRaw))
        {
            int splitIndex = filterRaw.IndexOf(':');

            if (splitIndex > 0)
            {
                filterType = NavigatorSearchFilterTypeExtensions.FromLegacyString(
                    filterRaw[..splitIndex]
                );
                filterValue = filterRaw[(splitIndex + 1)..];
            }
            else
            {
                filterValue = filterRaw;
            }
        }

        string filteringDataOut =
            filterType == NavigatorSearchFilterType.Anything
                ? filterValue
                : $"{filterType.ToLegacyString()}:{filterValue}";

        ImmutableArray<NavigatorSearchResultBlockSnapshot> blocks;

        if (searchCode == "categories" && string.IsNullOrEmpty(filterValue))
        {
            blocks = await _navigatorService.GetCategoryBlocksAsync(ct).ConfigureAwait(false);
        }
        else
        {
            ImmutableArray<NavigatorSearchResultSnapshot> searchResults = await _navigatorService
                .GetSearchResultsAsync(searchCode, filterType, filterValue, ctx.PlayerId, ct)
                .ConfigureAwait(false);

            int viewMode = await _grainFactory
                .GetPlayerNavigatorGrain(ctx.PlayerId)
                .GetViewModeAsync(searchCode, ct)
                .ConfigureAwait(false);

            blocks =
            [
                new NavigatorSearchResultBlockSnapshot
                {
                    SearchCode = searchCode,
                    Text = string.Empty,
                    ActionAllowed = NavigatorActionAllowedType.Back,
                    Localization = string.Empty,
                    ForceClosed = false,
                    ViewMode = (NavigatorViewModeType)viewMode,
                    Results = searchResults,
                },
            ];
        }

        await ctx.SendComposerAsync(
                new NavigatorSearchResultBlocksMessageComposer
                {
                    SearchCodeOriginal = searchCode,
                    FilteringData = filteringDataOut,
                    Blocks = blocks,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
