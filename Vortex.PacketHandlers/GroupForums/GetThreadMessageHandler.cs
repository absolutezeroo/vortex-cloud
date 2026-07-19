using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Groups.Snapshots;
using Vortex.Primitives.Messages.Incoming.GroupForums;
using Vortex.Primitives.Messages.Outgoing.Groupforums;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.GroupForums;

public class GetThreadMessageHandler(IGrainFactory grainFactory) : IMessageHandler<GetThreadMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        GetThreadMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || message.GroupId <= 0)
        {
            return;
        }

        ThreadMessagesPageSnapshot? page = await _grainFactory
            .GetGroupForumGrain(message.GroupId)
            .GetMessagesAsync(ctx.PlayerId, message.ThreadId, 0, 20, ct)
            .ConfigureAwait(false);

        if (page is null)
        {
            return;
        }

        await ctx.SendComposerAsync(new ThreadMessagesMessageComposer { Page = page }, ct)
            .ConfigureAwait(false);
    }
}
