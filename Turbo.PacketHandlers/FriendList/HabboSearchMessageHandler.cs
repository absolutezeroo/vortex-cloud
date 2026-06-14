using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.FriendList;
using Turbo.Primitives.Messages.Outgoing.FriendList;
using Turbo.Primitives.Orleans;

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
            return;

        var grain = _grainFactory.GetMessengerGrain(ctx.PlayerId);
        var directory = _grainFactory.GetPlayerDirectoryGrain();

        var friendResultsTask = grain.GetFriendSearchResultsAsync(message.SearchQuery, ct);
        var globalResultsTask = directory.SearchPlayersAsync(message.SearchQuery, SearchLimit, ct);

        var friendResults = await friendResultsTask.ConfigureAwait(false);
        var globalResults = await globalResultsTask.ConfigureAwait(false);

        // Friends who match go in the "friends" list, exclude them from "others"
        var friendIds = new HashSet<int>(friendResults.Select(f => f.PlayerId.Value));
        var otherResults = globalResults
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
