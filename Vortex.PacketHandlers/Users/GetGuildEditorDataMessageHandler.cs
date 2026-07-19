using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Groups.Snapshots;
using Vortex.Primitives.Messages.Incoming.Users;
using Vortex.Primitives.Messages.Outgoing.Users;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.Users;

public class GetGuildEditorDataMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GetGuildEditorDataMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        GetGuildEditorDataMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        GroupEditorDataSnapshot data = await _grainFactory
            .GetGroupDirectoryGrain()
            .GetEditorDataAsync(ct)
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(new GuildEditorDataMessageComposer { Data = data }, ct)
            .ConfigureAwait(false);
    }
}
