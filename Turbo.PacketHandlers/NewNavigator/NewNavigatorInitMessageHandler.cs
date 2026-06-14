using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.NewNavigator;
using Turbo.Primitives.Messages.Outgoing.NewNavigator;
using Turbo.Primitives.Navigator;
using Turbo.Primitives.Orleans;

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
        var navigatorGrain = _grainFactory.GetPlayerNavigatorGrain(ctx.PlayerId);

        var topLevelContexts = await _navigatorService
            .GetTopLevelContextAsync()
            .ConfigureAwait(false);

        var savedSearches = await navigatorGrain.GetSavedSearchesAsync(ct).ConfigureAwait(false);

        var collapsedCategories = await navigatorGrain
            .GetCollapsedCategoriesAsync(ct)
            .ConfigureAwait(false);

        var prefs = await navigatorGrain.GetWindowPreferencesAsync(ct).ConfigureAwait(false);

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
