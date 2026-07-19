using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Users;
using Vortex.Primitives.Messages.Outgoing.Users;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.Users;

public class KickMemberMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<KickMemberMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        KickMemberMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || message.GroupId <= 0)
        {
            return;
        }

        bool ok = await _grainFactory
            .GetGroupGrain(message.GroupId)
            .KickMemberAsync(ctx.PlayerId, message.UserId, message.Block, ct)
            .ConfigureAwait(false);

        // On success the client refreshes its own member list — nothing to send back.
        if (!ok)
        {
            await ctx.SendComposerAsync(
                    new GuildMemberMgmtFailedMessageComposer
                    {
                        GroupId = message.GroupId,
                        Reason = 0,
                    },
                    ct
                )
                .ConfigureAwait(false);
        }
    }
}
