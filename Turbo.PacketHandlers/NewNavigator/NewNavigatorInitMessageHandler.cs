using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.NewNavigator;
using Turbo.Primitives.Messages.Outgoing.NewNavigator;
using Turbo.Primitives.Navigator;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Orleans.Snapshots.Navigator;
using Turbo.Primitives.Players.Grains;

namespace Turbo.PacketHandlers.NewNavigator;

public class NewNavigatorInitMessageHandler(
    INavigatorService navigatorService,
    IGrainFactory grainFactory
) : IMessageHandler<NewNavigatorInitMessage>
{
    private readonly INavigatorService _navigatorService = navigatorService;
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        NewNavigatorInitMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        IPlayerNavigatorGrain navigatorGrain = _grainFactory.GetPlayerNavigatorGrain(ctx.PlayerId);

        ImmutableArray<NavigatorTopLevelContextSnapshot> topLevelContexts = await _navigatorService
            .GetTopLevelContextAsync()
            .ConfigureAwait(false);

        List<NavigatorQuickLinkSnapshot> savedSearches = await navigatorGrain.GetSavedSearchesAsync(ct).ConfigureAwait(false);

        List<string> collapsedCategories = await navigatorGrain
            .GetCollapsedCategoriesAsync(ct)
            .ConfigureAwait(false);

        NavigatorWindowPreferencesSnapshot prefs = await navigatorGrain.GetWindowPreferencesAsync(ct).ConfigureAwait(false);

        await ctx.SendComposerAsync(
                new NavigatorMetaDataMessage { TopLevelContexts = topLevelContexts },
                ct
            )
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(new NavigatorLiftedRoomsMessage { LiftedRooms = [] }, ct)
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(
                new NavigatorCollapsedCategoriesMessage
                {
                    CollapsedCategoryIds = collapsedCategories,
                },
                ct
            )
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(
                new NavigatorSavedSearchesMessage { SavedSearches = savedSearches },
                ct
            )
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(
                new NewNavigatorPreferencesMessageComposer
                {
                    WindowX = prefs.WindowX,
                    WindowY = prefs.WindowY,
                    WindowWidth = prefs.WindowWidth,
                    WindowHeight = prefs.WindowHeight,
                    LeftPaneHidden = prefs.LeftPaneHidden,
                    ResultsMode = prefs.ResultsMode,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
