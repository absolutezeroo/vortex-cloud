using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Groups.Snapshots;
using Vortex.Primitives.Messages.Incoming.Users;
using Vortex.Primitives.Messages.Outgoing.Users;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.Users;

public class GetGuildMembersMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GetGuildMembersMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        GetGuildMembersMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || message.GroupId <= 0)
        {
            return;
        }

        GroupMembersPageSnapshot? page = await _grainFactory
            .GetGroupGrain(message.GroupId)
            .GetMembersAsync(
                ctx.PlayerId,
                message.PageIndex,
                message.UserNameFilter,
                message.SearchType,
                ct
            )
            .ConfigureAwait(false);

        if (page is null)
        {
            return;
        }

        await ctx.SendComposerAsync(new GuildMembersMessageComposer { Page = page }, ct)
            .ConfigureAwait(false);
    }
}
