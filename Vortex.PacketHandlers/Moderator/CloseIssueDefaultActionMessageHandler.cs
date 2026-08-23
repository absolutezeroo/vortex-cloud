using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Events;
using Vortex.Primitives.Moderation;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Permissions;
using Vortex.Primitives.Players;
using Vortex.Protocol.Messages.Incoming.Moderator;
using Vortex.Protocol.Messages.Outgoing.Help;

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

        // A room report names a room and nobody, so there is no one for the topic's default sanction
        // to land on. Resolving permissions for player 0 would compare the actor against an empty
        // set and let the ban path run against nothing.
        if (ticket.Value.ReportedPlayerId <= 0)
        {
            logger.LogInformation(
                "CFH ticket {IssueId} has no reported player (room report); closing it without applying a default sanction.",
                message.PrimaryIssueId
            );

            await CloseWithoutSanctionAsync(message, ctx, ct).ConfigureAwait(false);

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

        ImmutableArray<CfhTicketCloseOutcome> outcomes = await grainFactory
            .GetModerationQueueGrain()
            .CloseAsync(
                PlayerId.Parse(ctx.PlayerId),
                allIssueIds,
                CfhTicketCloseReason.Sanctioned,
                sanctioned,
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
                        CloseReason = (int)CfhTicketCloseReason.Sanctioned,
                        MessageText = string.Empty,
                    }
                )
                .ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Closes the bundle as resolved and tells the reporters, with no sanction applied. Used when
    /// the ticket has nobody to sanction — a room report — so that the moderator's "close with the
    /// default action" still clears the queue instead of failing silently.
    /// </summary>
    private async Task CloseWithoutSanctionAsync(
        CloseIssueDefaultActionMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        List<int> allIssueIds = [message.PrimaryIssueId, .. message.OtherIssueIds];

        ImmutableArray<CfhTicketCloseOutcome> outcomes = await grainFactory
            .GetModerationQueueGrain()
            .CloseAsync(
                PlayerId.Parse(ctx.PlayerId),
                allIssueIds,
                CfhTicketCloseReason.Resolved,
                sanctioned: false,
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
                        CloseReason = (int)CfhTicketCloseReason.Resolved,
                        MessageText = string.Empty,
                    }
                )
                .ConfigureAwait(false);
        }
    }
}
