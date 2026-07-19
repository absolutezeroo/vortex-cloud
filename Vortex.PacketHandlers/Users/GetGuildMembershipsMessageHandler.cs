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

public class GetGuildMembershipsMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GetGuildMembershipsMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        GetGuildMembershipsMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        List<GuildInfoSnapshot> memberships = await _grainFactory
            .GetGroupDirectoryGrain()
            .GetMembershipsAsync(ctx.PlayerId, ct)
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(
                new GuildMembershipsMessageComposer { Memberships = memberships },
                ct
            )
            .ConfigureAwait(false);
    }
}
