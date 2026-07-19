using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Groups.Snapshots;
using Vortex.Primitives.Messages.Incoming.Users;
using Vortex.Primitives.Messages.Outgoing.Users;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.Users;

public class GetGuildEditInfoMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GetGuildEditInfoMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        GetGuildEditInfoMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || message.GroupId <= 0)
        {
            return;
        }

        GroupEditInfoSnapshot? info = await _grainFactory
            .GetGroupGrain(message.GroupId)
            .GetEditInfoAsync(ctx.PlayerId, ct)
            .ConfigureAwait(false);

        if (info is null)
        {
            return;
        }

        GroupEditorDataSnapshot editorData = await _grainFactory
            .GetGroupDirectoryGrain()
            .GetEditorDataAsync(ct)
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(new GuildEditorDataMessageComposer { Data = editorData }, ct)
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(new GuildEditInfoMessageComposer { Info = info }, ct)
            .ConfigureAwait(false);
    }
}
