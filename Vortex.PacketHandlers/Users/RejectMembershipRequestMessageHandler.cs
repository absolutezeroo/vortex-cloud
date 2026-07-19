using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Users;
using Vortex.Primitives.Messages.Outgoing.Users;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.Users;

public class RejectMembershipRequestMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<RejectMembershipRequestMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        RejectMembershipRequestMessage message,
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
            .RejectMembershipAsync(ctx.PlayerId, message.UserId, ct)
            .ConfigureAwait(false);

        if (ok)
        {
            await ctx.SendComposerAsync(
                    new GuildMembershipRejectedMessageComposer
                    {
                        GroupId = message.GroupId,
                        UserId = message.UserId,
                    },
                    ct
                )
                .ConfigureAwait(false);
        }
        else
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
