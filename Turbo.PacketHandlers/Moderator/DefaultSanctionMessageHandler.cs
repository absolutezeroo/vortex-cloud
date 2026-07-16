using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Events;
using Turbo.Primitives.Messages.Incoming.Moderator;
using Turbo.Primitives.Messages.Outgoing.Help;
using Turbo.Primitives.Moderation;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Permissions;

namespace Turbo.PacketHandlers.Moderator;

public class DefaultSanctionMessageHandler(
    IGrainFactory grainFactory,
    IPermissionService permissionService,
    ISessionGateway sessionGateway,
    ISanctionPresetService sanctionPresets,
    ICfhTicketService tickets,
    IEventPublisher events,
    ILogger<DefaultSanctionMessageHandler> logger
) : IMessageHandler<DefaultSanctionMessage>
{
    public async ValueTask HandleAsync(
        DefaultSanctionMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || message.UserId <= 0)
        {
            return;
        }

        PermissionSet actorPermissions = await permissionService
            .ResolveForPlayerAsync(ctx.PlayerId, ct)
            .ConfigureAwait(false);
        PermissionSet targetPermissions = await permissionService
            .ResolveForPlayerAsync(message.UserId, ct)
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
                        message.UserId,
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
                "DefaultSanction: topic {TopicId} has no default sanction preset configured; skipping.",
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
                message.UserId,
                ctx.PlayerId,
                message.Message,
                ct
            )
            .ConfigureAwait(false);

        if (message.IssueId < 0)
        {
            return;
        }

        ImmutableArray<CfhTicketCloseOutcome> outcomes = await tickets
            .CloseTicketsAsync([message.IssueId], CfhTicketCloseReason.Sanctioned, sanctioned, ct)
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
