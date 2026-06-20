using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.FriendList.Grains;
using Turbo.Primitives.Messages.Incoming.FriendList;
using Turbo.Primitives.Messages.Outgoing.FriendList;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Players;
using Turbo.Primitives.Snapshots.FriendList;

namespace Turbo.PacketHandlers.FriendList;

public class GetMessengerHistoryMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GetMessengerHistoryMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        GetMessengerHistoryMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        IMessengerGrain grain = _grainFactory.GetMessengerGrain(ctx.PlayerId);
        PlayerId friendId = PlayerId.Parse(message.ChatId);

        List<MessageHistoryEntrySnapshot> history = await grain
            .GetMessageHistoryAsync(friendId, ct)
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(
                new ConsoleMessageHistoryMessageComposer
                {
                    ChatId = message.ChatId,
                    Messages = history,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
