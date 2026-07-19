using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.FriendList;
using Vortex.Primitives.Messages.Outgoing.FriendList;

namespace Vortex.PacketHandlers.FriendList;

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
            )
            .ConfigureAwait(false);
    }
}
