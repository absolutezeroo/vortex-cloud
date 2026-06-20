using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Groups.Snapshots;
using Turbo.Primitives.Messages.Incoming.Users;
using Turbo.Primitives.Messages.Outgoing.Users;
using Turbo.Primitives.Orleans;

namespace Turbo.PacketHandlers.Users;

public class GetHabboGroupDetailsMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GetHabboGroupDetailsMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        GetHabboGroupDetailsMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || message.GroupId <= 0)
        {
            return;
        }

        GroupDetailsSnapshot? details = await _grainFactory
            .GetGroupGrain(message.GroupId)
            .GetDetailsAsync(ctx.PlayerId, ct)
            .ConfigureAwait(false);

        if (details is null)
        {
            return;
        }

        await ctx.SendComposerAsync(new HabboGroupDetailsMessageComposer { Details = details }, ct)
            .ConfigureAwait(false);
    }
}
