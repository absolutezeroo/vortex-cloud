using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.NewNavigator;
using Vortex.Primitives.Messages.Outgoing.NewNavigator;
using Vortex.Primitives.Navigator;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Orleans.Snapshots.Navigator;
using Vortex.Primitives.Players.Grains;

namespace Vortex.PacketHandlers.NewNavigator;

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

        List<NavigatorQuickLinkSnapshot> savedSearches = await navigatorGrain
            .GetSavedSearchesAsync(ct)
            .ConfigureAwait(false);

        List<string> collapsedCategories = await navigatorGrain
            .GetCollapsedCategoriesAsync(ct)
            .ConfigureAwait(false);

        NavigatorWindowPreferencesSnapshot prefs = await navigatorGrain
            .GetWindowPreferencesAsync(ct)
            .ConfigureAwait(false);

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
