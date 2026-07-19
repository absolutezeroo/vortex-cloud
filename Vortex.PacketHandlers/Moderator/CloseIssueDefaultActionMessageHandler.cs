using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Events;
using Vortex.Primitives.Messages.Incoming.Moderator;
using Vortex.Primitives.Messages.Outgoing.Help;
using Vortex.Primitives.Moderation;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Permissions;

namespace Vortex.PacketHandlers.Moderator;

public class CloseIssueDefaultActionMessageHandler(
    IGrainFactory grainFactory,
    IPermissionService permissionService,
    ISessionGateway sessionGateway,
    ISanctionPresetService sanctionPresets,
    ICfhTicketService tickets,
    IEventPublisher events,
    ILogger<CloseIssueDefaultActionMessageHandler> logger
) : IMessageHandler<CloseIssueDefaultActionMessage>
{
    public async ValueTask HandleAsync(
        CloseIssueDefaultActionMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || message.PrimaryIssueId <= 0)
        {
            return;
        }

        CfhTicketSummary? ticket = await tickets
            .GetTicketAsync(message.PrimaryIssueId, ct)
            .ConfigureAwait(false);

        if (ticket is null)
        {
            return;
        }

        PermissionSet actorPermissions = await permissionService
            .ResolveForPlayerAsync(ctx.PlayerId, ct)
            .ConfigureAwait(false);
        PermissionSet targetPermissions = await permissionService
            .ResolveForPlayerAsync(ticket.Value.ReportedPlayerId, ct)
            .ConfigureAwait(false);

        if (
            !actorPermissions.HasAny(Capabilities.Moderation.Cfh)
            || !ModerationPolicy.IsAllowed(
                actorPermissions,
                targetPermissions,
                ModerationAction.Ban
            )
        )
        {
            await events
                .PublishAsync(
                    new ModerationActionDeniedEvent(
                        ctx.PlayerId,
                        ticket.Value.ReportedPlayerId,
                        ctx.RoomId,
                        nameof(ModerationAction.Ban)
                    ),
                    ct
                )
                .ConfigureAwait(false);

            return;
        }

        CfhTopicSnapshot? topic = await tickets
            .GetTopicAsync(message.TopicId, ct)
            .ConfigureAwait(false);

        if (topic is null || topic.Value.DefaultSanctionPresetId is null)
        {
            logger.LogWarning(
                "CloseIssueDefaultAction: topic {TopicId} has no default sanction preset configured; skipping.",
                message.TopicId
            );

            return;
        }

        bool sanctioned = await CfhDefaultSanctionHelper
            .ApplyAsync(
                grainFactory,
                sessionGateway,
                sanctionPresets,
                topic.Value.DefaultSanctionPresetId.Value,
                ticket.Value.ReportedPlayerId,
                ctx.PlayerId,
                topic.Value.Consequence ?? topic.Value.Name,
                ct
            )
            .ConfigureAwait(false);

        List<int> allIssueIds = [message.PrimaryIssueId, .. message.OtherIssueIds];

        ImmutableArray<CfhTicketCloseOutcome> outcomes = await tickets
            .CloseTicketsAsync(allIssueIds, CfhTicketCloseReason.Sanctioned, sanctioned, ct)
            .ConfigureAwait(false);

        foreach (CfhTicketCloseOutcome outcome in outcomes)
        {
            await grainFactory
                .GetPlayerPresenceGrain(outcome.ReporterPlayerId)
                .SendComposerAsync(
                    new IssueCloseNotificationMessageComposer
                    {
                        CloseReason = (int)CfhTicketCloseReason.Sanctioned,
                        MessageText = string.Empty,
                    }
                )
                .ConfigureAwait(false);
        }
    }
}
