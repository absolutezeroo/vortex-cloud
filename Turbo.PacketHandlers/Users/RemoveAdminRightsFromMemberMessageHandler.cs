using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Users;
using Turbo.Primitives.Messages.Outgoing.Users;
using Turbo.Primitives.Orleans;

namespace Turbo.PacketHandlers.Users;

public class RemoveAdminRightsFromMemberMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<RemoveAdminRightsFromMemberMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        RemoveAdminRightsFromMemberMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || message.GroupId <= 0)
            return;

        var member = await _grainFactory
            .GetGroupGrain(message.GroupId)
            .SetAdminRightsAsync(ctx.PlayerId, message.UserId, false, ct)
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
