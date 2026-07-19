using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Groups.Snapshots;
using Vortex.Primitives.Messages.Incoming.Users;
using Vortex.Primitives.Messages.Outgoing.Users;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.Users;

public class GetGuildCreationInfoMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GetGuildCreationInfoMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        GetGuildCreationInfoMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        GroupEditorDataSnapshot editorData = await _grainFactory
            .GetGroupDirectoryGrain()
            .GetEditorDataAsync(ct)
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(new GuildEditorDataMessageComposer { Data = editorData }, ct)
            .ConfigureAwait(false);

        GroupCreationInfoSnapshot info = await _grainFactory
            .GetGroupDirectoryGrain()
            .GetCreationInfoAsync(ctx.PlayerId, ct)
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(new GuildCreationInfoMessageComposer { Info = info }, ct)
            .ConfigureAwait(false);
    }
}
