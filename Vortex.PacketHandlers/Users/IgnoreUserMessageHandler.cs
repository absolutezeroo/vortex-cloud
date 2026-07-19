using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.FriendList.Grains;
using Vortex.Primitives.Messages.Incoming.Users;
using Vortex.Primitives.Messages.Outgoing.Users;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players;

namespace Vortex.PacketHandlers.Users;

public class IgnoreUserMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<IgnoreUserMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        IgnoreUserMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        IMessengerGrain grain = _grainFactory.GetMessengerGrain(ctx.PlayerId);
        await grain.IgnoreUserAsync(PlayerId.Parse(message.UserId), ct).ConfigureAwait(false);

        await ctx.SendComposerAsync(
                new IgnoreResultMessageComposer { UserId = message.UserId, ResultCode = 1 },
                ct
            )
            .ConfigureAwait(false);
    }
}
