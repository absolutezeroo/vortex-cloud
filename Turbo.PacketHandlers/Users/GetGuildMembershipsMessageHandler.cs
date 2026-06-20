using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Groups.Snapshots;
using Turbo.Primitives.Messages.Incoming.Users;
using Turbo.Primitives.Messages.Outgoing.Users;
using Turbo.Primitives.Orleans;

namespace Turbo.PacketHandlers.Users;

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
