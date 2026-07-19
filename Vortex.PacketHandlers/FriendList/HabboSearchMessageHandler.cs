using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.PacketHandlers.Configuration;
using Vortex.Primitives.FriendList.Grains;
using Vortex.Primitives.Messages.Incoming.FriendList;
using Vortex.Primitives.Messages.Outgoing.FriendList;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players.Grains;
using Vortex.Primitives.Snapshots.FriendList;

namespace Vortex.PacketHandlers.FriendList;

public class HabboSearchMessageHandler(
    IGrainFactory grainFactory,
    IOptions<FriendListConfig> friendListConfig
) : IMessageHandler<HabboSearchMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;
    private readonly FriendListConfig _friendListConfig = friendListConfig.Value;

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
            _friendListConfig.SearchLimit,
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
            .Take(_friendListConfig.SearchLimit)
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
