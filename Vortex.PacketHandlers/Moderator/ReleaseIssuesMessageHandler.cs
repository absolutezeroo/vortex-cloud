using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Permissions;
using Vortex.Primitives.Players;
using Vortex.Protocol.Messages.Incoming.Moderator;

namespace Vortex.PacketHandlers.Moderator;

public class ReleaseIssuesMessageHandler(
    IGrainFactory grainFactory,
    IPermissionService permissionService
) : IMessageHandler<ReleaseIssuesMessage>
{
    public async ValueTask HandleAsync(
        ReleaseIssuesMessage message,
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

        // Routed through the queue grain so the released tickets go back on every other
        // moderator's list, not just out of this one's.
        await grainFactory
            .GetModerationQueueGrain()
            .ReleaseAsync(PlayerId.Parse(ctx.PlayerId), message.IssueIds, ct)
            .ConfigureAwait(false);
    }
}
