using System.Threading;
using System.Threading.Tasks;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.FriendList;
using Turbo.Primitives.Messages.Outgoing.FriendList;

namespace Turbo.PacketHandlers.FriendList;

public class FindNewFriendsMessageHandler : IMessageHandler<FindNewFriendsMessage>
{
    public async ValueTask HandleAsync(
        FindNewFriendsMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ctx.SendComposerAsync(
            new FindFriendsProcessResultMessageComposer { Success = true },
            ct
        ).ConfigureAwait(false);
    }
}
