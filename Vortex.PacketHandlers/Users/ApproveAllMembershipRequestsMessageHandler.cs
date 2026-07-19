using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Groups.Snapshots;
using Vortex.Primitives.Messages.Incoming.Users;
using Vortex.Primitives.Messages.Outgoing.Users;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.Users;

public class ApproveAllMembershipRequestsMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<ApproveAllMembershipRequestsMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        ApproveAllMembershipRequestsMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || message.GroupId <= 0)
        {
            return;
        }

        List<GroupMemberSnapshot> added = await _grainFactory
            .GetGroupGrain(message.GroupId)
            .ApproveAllMembershipsAsync(ctx.PlayerId, ct)
            .ConfigureAwait(false);

        foreach (GroupMemberSnapshot member in added)
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
    }
}
