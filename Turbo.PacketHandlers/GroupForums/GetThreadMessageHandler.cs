using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Groups.Snapshots;
using Turbo.Primitives.Messages.Incoming.GroupForums;
using Turbo.Primitives.Messages.Outgoing.Groupforums;
using Turbo.Primitives.Orleans;

namespace Turbo.PacketHandlers.GroupForums;

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
