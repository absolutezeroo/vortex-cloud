using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Groups.Snapshots;
using Vortex.Primitives.Messages.Incoming.Users;
using Vortex.Primitives.Messages.Outgoing.Users;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.Users;

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

        GroupEditorDataSnapshot editorData = await _grainFactory
            .GetGroupDirectoryGrain()
            .GetEditorDataAsync(ct)
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(new GuildEditorDataMessageComposer { Data = editorData }, ct)
            .ConfigureAwait(false);

        details = details with { OpenDetails = message.RequestDetails };

        await ctx.SendComposerAsync(new HabboGroupDetailsMessageComposer { Details = details }, ct)
            .ConfigureAwait(false);
    }
}
