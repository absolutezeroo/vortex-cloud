using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Users;
using Turbo.Primitives.Messages.Outgoing.Users;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Players;

namespace Turbo.PacketHandlers.Users;

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
            return;

        var grain = _grainFactory.GetMessengerGrain(ctx.PlayerId);
        await grain.IgnoreUserAsync(PlayerId.Parse(message.UserId), ct).ConfigureAwait(false);

        await ctx.SendComposerAsync(
                new IgnoreResultMessageComposer { UserId = message.UserId, ResultCode = 1 },
                ct
            )
            .ConfigureAwait(false);
    }
}
