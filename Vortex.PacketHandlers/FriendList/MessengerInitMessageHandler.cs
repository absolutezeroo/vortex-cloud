using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Vortex.Logging.Extensions;
using Vortex.Messages.Registry;
using Vortex.PacketHandlers.Configuration;
using Vortex.Primitives.FriendList.Grains;
using Vortex.Primitives.Messages.Incoming.FriendList;
using Vortex.Primitives.Messages.Outgoing.FriendList;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Server.Grains;
using Vortex.Primitives.Snapshots.FriendList;

namespace Vortex.PacketHandlers.FriendList;

public class MessengerInitMessageHandler(
    IGrainFactory grainFactory,
    ILogger<MessengerInitMessageHandler> logger
) : IMessageHandler<MessengerInitMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;
    private readonly ILogger<MessengerInitMessageHandler> _logger = logger;

    public async ValueTask HandleAsync(
        MessengerInitMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        IMessengerGrain grain = _grainFactory.GetMessengerGrain(ctx.PlayerId);
        IServerConfigGrain config = _grainFactory.GetServerConfigGrain();

        // Kick off everything up front so the limit lookups overlap the friend/category loads.
        Task<List<FriendCategorySnapshot>> categoriesTask = grain.GetCategoriesAsync(ct);
        Task<List<MessengerFriendSnapshot>> friendsTask = grain.GetFriendsAsync(ct);
        Task<int> userLimitTask = config.GetIntAsync(
            FriendListConfig.UserFriendLimitKey,
            FriendListConfig.UserFriendLimitDefault
        );
        Task<int> normalLimitTask = config.GetIntAsync(
            FriendListConfig.NormalFriendLimitKey,
            FriendListConfig.NormalFriendLimitDefault
        );
        Task<int> extendedLimitTask = config.GetIntAsync(
            FriendListConfig.ExtendedFriendLimitKey,
            FriendListConfig.ExtendedFriendLimitDefault
        );
        Task<int> fragmentSizeTask = config.GetIntAsync(
            FriendListConfig.FragmentSizeKey,
            FriendListConfig.FragmentSizeDefault
        );

        List<FriendCategorySnapshot> categories = await categoriesTask.ConfigureAwait(false);
        List<MessengerFriendSnapshot> friends = await friendsTask.ConfigureAwait(false);

        await ctx.SendComposerAsync(
                new MessengerInitMessageComposer
                {
                    UserFriendLimit = await userLimitTask.ConfigureAwait(false),
                    NormalFriendLimit = await normalLimitTask.ConfigureAwait(false),
                    ExtendedFriendLimit = await extendedLimitTask.ConfigureAwait(false),
                    FriendCategories = categories,
                },
                ct
            )
            .ConfigureAwait(false);

        // Guard against a misconfigured 0/negative size that would break the fragment maths.
        int fragmentSize = Math.Max(1, await fragmentSizeTask.ConfigureAwait(false));
        int totalFragments = Math.Max(1, (int)Math.Ceiling(friends.Count / (double)fragmentSize));

        for (int i = 0; i < totalFragments; i++)
        {
            List<MessengerFriendSnapshot> fragment = friends.GetRange(
                i * fragmentSize,
                Math.Min(fragmentSize, friends.Count - i * fragmentSize)
            );

            await ctx.SendComposerAsync(
                    new FriendListFragmentMessageComposer
                    {
                        TotalFragments = totalFragments,
                        FragmentIndex = i,
                        Fragment = fragment,
                    },
                    ct
                )
                .ConfigureAwait(false);
        }

        // Notify online — delivers pending messages and notifies friends fire-and-forget
        grain
            .NotifyOnlineAsync(ct)
            .LogAndForget(_logger, "Failed to notify online for player {PlayerId}", ctx.PlayerId);
    }
}
