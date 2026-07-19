using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Groups.Snapshots;
using Vortex.Primitives.Messages.Incoming.GroupForums;
using Vortex.Primitives.Messages.Outgoing.Groupforums;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.GroupForums;

public class UpdateThreadMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<UpdateThreadMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        UpdateThreadMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || message.GroupId <= 0)
        {
            return;
        }

        ForumThreadSnapshot? thread = await _grainFactory
            .GetGroupForumGrain(message.GroupId)
            .UpdateThreadAsync(
                ctx.PlayerId,
                message.ThreadId,
                message.IsLocked,
                message.IsSticky,
                ct
            )
            .ConfigureAwait(false);

        if (thread is null)
        {
            return;
        }

        await ctx.SendComposerAsync(
                new UpdateThreadMessageComposer { GroupId = message.GroupId, Thread = thread },
                ct
            )
            .ConfigureAwait(false);
    }
}
