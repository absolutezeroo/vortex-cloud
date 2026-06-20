using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.FriendList.Grains;
using Turbo.Primitives.Messages.Incoming.FriendList;
using Turbo.Primitives.Messages.Outgoing.FriendList;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Players.Grains;
using Turbo.Primitives.Snapshots.FriendList;

namespace Turbo.PacketHandlers.FriendList;

public class HabboSearchMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<HabboSearchMessage>
{
    private const int SearchLimit = 30;
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        HabboSearchMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || string.IsNullOrWhiteSpace(message.SearchQuery))
        {
            return;
        }

        IMessengerGrain grain = _grainFactory.GetMessengerGrain(ctx.PlayerId);
        IPlayerDirectoryGrain directory = _grainFactory.GetPlayerDirectoryGrain();

        Task<List<MessengerSearchResultSnapshot>> friendResultsTask =
            grain.GetFriendSearchResultsAsync(message.SearchQuery, ct);
        Task<List<MessengerSearchResultSnapshot>> globalResultsTask = directory.SearchPlayersAsync(
            message.SearchQuery,
            SearchLimit,
            ct
        );

        List<MessengerSearchResultSnapshot> friendResults = await friendResultsTask.ConfigureAwait(
            false
        );
        List<MessengerSearchResultSnapshot> globalResults = await globalResultsTask.ConfigureAwait(
            false
        );

        // Friends who match go in the "friends" list, exclude them from "others"
        HashSet<int> friendIds = new HashSet<int>(friendResults.Select(f => f.PlayerId.Value));
        List<MessengerSearchResultSnapshot> otherResults = globalResults
            .Where(r =>
                !friendIds.Contains(r.PlayerId.Value) && r.PlayerId.Value != ctx.PlayerId.Value
            )
            .Take(SearchLimit)
            .ToList();

        await ctx.SendComposerAsync(
                new HabboSearchResultMessageComposer
                {
                    Friends = friendResults,
                    Others = otherResults,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
