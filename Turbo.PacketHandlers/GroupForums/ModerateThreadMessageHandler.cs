using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Groups.Snapshots;
using Turbo.Primitives.Messages.Incoming.GroupForums;
using Turbo.Primitives.Messages.Outgoing.Groupforums;
using Turbo.Primitives.Orleans;

namespace Turbo.PacketHandlers.GroupForums;

public class ModerateThreadMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<ModerateThreadMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        ModerateThreadMessage message,
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
            .ModerateThreadAsync(ctx.PlayerId, message.ThreadId, message.Action, ct)
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
