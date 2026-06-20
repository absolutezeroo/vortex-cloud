using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Users;
using Turbo.Primitives.Messages.Outgoing.Users;
using Turbo.Primitives.Orleans;

namespace Turbo.PacketHandlers.Users;

public class JoinHabboGroupMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<JoinHabboGroupMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        JoinHabboGroupMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || message.GroupId <= 0)
            return;

        var grain = _grainFactory.GetGroupGrain(message.GroupId);

        var failureReason = await grain.JoinAsync(ctx.PlayerId, ct).ConfigureAwait(false);

        if (failureReason is not null)
        {
            await ctx.SendComposerAsync(
                    new HabboGroupJoinFailedMessageComposer { Reason = failureReason.Value },
                    ct
                )
                .ConfigureAwait(false);
            return;
        }

        // Refresh the detail view so membership status / counts update client-side.
        var details = await grain.GetDetailsAsync(ctx.PlayerId, ct).ConfigureAwait(false);
        if (details is not null)
        {
            await ctx.SendComposerAsync(
                    new HabboGroupDetailsMessageComposer { Details = details },
                    ct
                )
                .ConfigureAwait(false);
        }
    }
}
