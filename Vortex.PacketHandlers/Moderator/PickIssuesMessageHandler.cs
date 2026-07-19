using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Moderator;
using Vortex.Primitives.Moderation;
using Vortex.Primitives.Permissions;

namespace Vortex.PacketHandlers.Moderator;

public class PickIssuesMessageHandler(
    IPermissionService permissionService,
    ICfhTicketService tickets
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

        await tickets.PickTicketsAsync(message.IssueIds, ctx.PlayerId, ct).ConfigureAwait(false);
    }
}
