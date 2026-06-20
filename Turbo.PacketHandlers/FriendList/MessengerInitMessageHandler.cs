using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.FriendList.Grains;
using Turbo.Primitives.Messages.Incoming.FriendList;
using Turbo.Primitives.Messages.Outgoing.FriendList;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Snapshots.FriendList;

namespace Turbo.PacketHandlers.FriendList;

public class MessengerInitMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<MessengerInitMessage>
{
    private const int FragmentSize = 500;
    private const int UserFriendLimit = 300;
    private const int NormalFriendLimit = 300;
    private const int ExtendedFriendLimit = 2000;

    private readonly IGrainFactory _grainFactory = grainFactory;

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

        Task<List<FriendCategorySnapshot>> categoriesTask = grain.GetCategoriesAsync(ct);
        Task<List<MessengerFriendSnapshot>> friendsTask = grain.GetFriendsAsync(ct);

        List<FriendCategorySnapshot> categories = await categoriesTask.ConfigureAwait(false);
        List<MessengerFriendSnapshot> friends = await friendsTask.ConfigureAwait(false);

        await ctx.SendComposerAsync(
                new MessengerInitMessageComposer
                {
                    UserFriendLimit = UserFriendLimit,
                    NormalFriendLimit = NormalFriendLimit,
                    ExtendedFriendLimit = ExtendedFriendLimit,
                    FriendCategories = categories,
                },
                ct
            )
            .ConfigureAwait(false);

        // Fragment the friend list (max 500 per packet)
        int totalFragments = Math.Max(1, (int)Math.Ceiling(friends.Count / (double)FragmentSize));

        for (int i = 0; i < totalFragments; i++)
        {
            List<MessengerFriendSnapshot> fragment = friends.GetRange(
                i * FragmentSize,
                Math.Min(FragmentSize, friends.Count - i * FragmentSize)
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
        _ = grain.NotifyOnlineAsync(ct);
    }
}
