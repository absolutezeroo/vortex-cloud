using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Moderator;
using Vortex.Primitives.Moderation;
using Vortex.Primitives.Permissions;

namespace Vortex.PacketHandlers.Moderator;

public class ReleaseIssuesMessageHandler(
    IPermissionService permissionService,
    ICfhTicketService tickets
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

        await tickets.ReleaseTicketsAsync(message.IssueIds, ct).ConfigureAwait(false);
    }
}
