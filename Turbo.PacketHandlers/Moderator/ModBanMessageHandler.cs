using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Events;
using Turbo.Primitives.Messages.Incoming.Moderator;
using Turbo.Primitives.Messages.Outgoing.Moderation;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Permissions;
using Turbo.Primitives.Players;
using Turbo.Primitives.Players.Grains;
using Turbo.Primitives.Rooms;

namespace Turbo.PacketHandlers.Moderator;

public class ModBanMessageHandler(
    IGrainFactory grainFactory,
    IPermissionService permissionService,
    ISanctionPresetService sanctionPresets,
    IEventPublisher events,
    ISessionGateway sessionGateway,
    ILogger<ModBanMessageHandler> logger
) : IMessageHandler<ModBanMessage>
{
    public async ValueTask HandleAsync(
        ModBanMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || message.UserId <= 0)
        {
            return;
        }

        RoomId targetRoomId = await ModToolActionHelper
            .GetTargetRoomIdAsync(grainFactory, message.UserId)
            .ConfigureAwait(false);

        bool success = false;

        if (
            await ModToolActionHelper
                .IsAuthorizedAsync(
                    permissionService,
                    events,
                    ctx.PlayerId,
                    message.UserId,
                    targetRoomId,
                    ModerationAction.Ban,
                    ct
                )
                .ConfigureAwait(false)
        )
        {
            SanctionPresetSnapshot? preset = await sanctionPresets
                .ResolveAsync(SanctionPresetKind.Ban, message.SanctionTypeId, ct)
                .ConfigureAwait(false);

            if (preset is null)
            {
                // Unrecognized preset index: fail loudly (success stays false) rather than either
                // guessing a duration or silently no-op'ing under a "success" ack.
                logger.LogWarning(
                    "No SanctionPresetEntity configured for Ban preset index {SanctionTypeId}; ban rejected.",
                    message.SanctionTypeId
                );
            }
            else
            {
                DateTime bannedUntil = preset.Value.DurationSeconds is null
                    ? SanctionDuration.Permanent
                    : DateTime.UtcNow.AddSeconds(preset.Value.DurationSeconds.Value);

                IPlayerGrain targetGrain = grainFactory.GetPlayerGrain(message.UserId);

                success = await targetGrain
                    .ApplyAccountBanAsync(ctx.PlayerId, bannedUntil, message.Message, ct)
                    .ConfigureAwait(false);

                if (success)
                {
                    await grainFactory
                        .GetPlayerPresenceGrain(message.UserId)
                        .SendComposerAsync(
                            new UserBannedMessageComposer { Message = message.Message }
                        )
                        .ConfigureAwait(false);
                    await sessionGateway
                        .RemoveSessionFromPlayerAsync(message.UserId, ct)
                        .ConfigureAwait(false);
                }
            }
        }

        await ModToolActionHelper
            .SendResultAsync(grainFactory, ctx.PlayerId, message.UserId, success)
            .ConfigureAwait(false);
    }
}
