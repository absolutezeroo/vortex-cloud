using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Groups.Grains;
using Vortex.Primitives.Groups.Snapshots;
using Vortex.Protocol.Messages.Incoming.Users;
using Vortex.Protocol.Messages.Outgoing.Users;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.Users;

/// <summary>
/// Lifts a guild rejoin block applied by kicking a member with the "block" flag, then re-sends the
/// guild details so the open management window reflects the change.
/// </summary>
public class UnblockGroupMemberMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<UnblockGroupMemberMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        UnblockGroupMemberMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || message.GroupId <= 0 || message.UserId <= 0)
        {
            return;
        }

        IGroupGrain groupGrain = _grainFactory.GetGroupGrain(message.GroupId);

        bool unblocked = await groupGrain
            .UnblockMemberAsync(ctx.PlayerId, message.UserId, ct)
            .ConfigureAwait(false);

        if (!unblocked)
        {
            return;
        }

        GroupDetailsSnapshot? details = await groupGrain
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
