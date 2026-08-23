using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Permissions;
using Vortex.Primitives.Players;
using Vortex.Protocol.Messages.Incoming.Moderator;

namespace Vortex.PacketHandlers.Moderator;

public class PickIssuesMessageHandler(
    IGrainFactory grainFactory,
    IPermissionService permissionService
) : IMessageHandler<PickIssuesMessage>
{
    public async ValueTask HandleAsync(
        PickIssuesMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || message.IssueIds.IsDefaultOrEmpty)
        {
            return;
        }

        PermissionSet permissions = await permissionService
            .ResolveForPlayerAsync(ctx.PlayerId, ct)
            .ConfigureAwait(false);

        if (!permissions.HasAny(Capabilities.Moderation.Cfh))
        {
            return;
        }

        // Through the queue grain, never straight to the service: two moderators auto-picking hit
        // this at the same instant, and the grain's turn is what stops them both winning.
        await grainFactory
            .GetModerationQueueGrain()
            .PickAsync(
                PlayerId.Parse(ctx.PlayerId),
                message.IssueIds,
                message.RetryEnabled,
                message.RetryCount,
                ct
            )
            .ConfigureAwait(false);
    }
}
