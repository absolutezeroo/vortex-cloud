using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Groups.Snapshots;
using Turbo.Primitives.Messages.Incoming.Users;
using Turbo.Primitives.Messages.Outgoing.Users;
using Turbo.Primitives.Orleans;

namespace Turbo.PacketHandlers.Users;

public class ApproveMembershipRequestMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<ApproveMembershipRequestMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        ApproveMembershipRequestMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || message.GroupId <= 0)
        {
            return;
        }

        GroupMemberSnapshot? member = await _grainFactory
            .GetGroupGrain(message.GroupId)
            .ApproveMembershipAsync(ctx.PlayerId, message.UserId, ct)
            .ConfigureAwait(false);

        if (member is not null)
        {
            await ctx.SendComposerAsync(
                    new GuildMembershipUpdatedMessageComposer
                    {
                        GroupId = message.GroupId,
                        Member = member,
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
