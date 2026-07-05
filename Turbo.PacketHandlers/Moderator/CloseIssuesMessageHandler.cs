using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Moderator;
using Turbo.Primitives.Messages.Outgoing.Help;
using Turbo.Primitives.Moderation;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Permissions;

namespace Turbo.PacketHandlers.Moderator;

public class CloseIssuesMessageHandler(
    IGrainFactory grainFactory,
    IPermissionService permissionService,
    ICfhTicketService tickets
) : IMessageHandler<CloseIssuesMessage>
{
    public async ValueTask HandleAsync(
        CloseIssuesMessage message,
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

        // Matches the client's own CallForHelpManager.getCloseReasonKey: 1 = useless, 2 = abusive,
        // anything else = resolved.
        CfhTicketCloseReason reason = message.CloseReason switch
        {
            1 => CfhTicketCloseReason.Useless,
            2 => CfhTicketCloseReason.Sanctioned,
            _ => CfhTicketCloseReason.Resolved,
        };

        ImmutableArray<CfhTicketCloseOutcome> outcomes = await tickets
            .CloseTicketsAsync(
                message.IssueIds,
                reason,
                sanctioned: reason == CfhTicketCloseReason.Sanctioned,
                ct
            )
            .ConfigureAwait(false);

        foreach (CfhTicketCloseOutcome outcome in outcomes)
        {
            await grainFactory
                .GetPlayerPresenceGrain(outcome.ReporterPlayerId)
                .SendComposerAsync(
                    new IssueCloseNotificationMessageComposer
                    {
                        CloseReason = message.CloseReason,
                        MessageText = string.Empty,
                    }
                )
                .ConfigureAwait(false);
        }
    }
}
