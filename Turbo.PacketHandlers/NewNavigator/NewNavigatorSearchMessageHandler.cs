using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.NewNavigator;
using Turbo.Primitives.Messages.Outgoing.NewNavigator;
using Turbo.Primitives.Navigator;
using Turbo.Primitives.Navigator.Enums;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Orleans.Snapshots.Navigator;

namespace Turbo.PacketHandlers.NewNavigator;

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
        var searchCode = message.SearchCodeOriginal ?? string.Empty;
        var filterRaw = message.FilteringData ?? string.Empty;

        NavigatorSearchFilterType filterType = NavigatorSearchFilterType.Anything;
        string filterValue = string.Empty;

        if (!string.IsNullOrWhiteSpace(filterRaw))
        {
            var splitIndex = filterRaw.IndexOf(':');

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

        var filteringDataOut =
            filterType == NavigatorSearchFilterType.Anything
                ? filterValue
                : $"{filterType.ToLegacyString()}:{filterValue}";

        ImmutableArray<NavigatorSearchResultBlockSnapshot> blocks;

        if (searchCode == "categories" && string.IsNullOrEmpty(filterValue))
        {
            blocks = await _navigatorService
                .GetCategoryBlocksAsync(ct)
                .ConfigureAwait(false);
        }
        else
        {
            var searchResults = await _navigatorService
                .GetSearchResultsAsync(searchCode, filterType, filterValue, ctx.PlayerId, ct)
                .ConfigureAwait(false);

            var viewMode = await _grainFactory
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
