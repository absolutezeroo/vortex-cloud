using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.FriendList.Grains;
using Vortex.Primitives.Messages.Incoming.FriendList;
using Vortex.Primitives.Messages.Outgoing.FriendList;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players;
using Vortex.Primitives.Snapshots.FriendList;

namespace Vortex.PacketHandlers.FriendList;

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
