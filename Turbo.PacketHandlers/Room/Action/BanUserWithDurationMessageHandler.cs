using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Events;
using Turbo.Primitives.Messages.Incoming.Room.Action;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Permissions;
using Turbo.Primitives.Rooms;

namespace Turbo.PacketHandlers.Room.Action;

public class BanUserWithDurationMessageHandler(
    IGrainFactory grainFactory,
    IPermissionService permissionService,
    IEventPublisher events
) : IMessageHandler<BanUserWithDurationMessage>
{
    public async ValueTask HandleAsync(BanUserWithDurationMessage message, MessageContext ctx, CancellationToken ct)
    {
        if (ctx.PlayerId <= 0 || message.UserId <= 0)
            return;

        RoomId actorRoomId = message.RoomId > 0 ? new RoomId(message.RoomId) : ctx.RoomId;
        if (actorRoomId <= 0)
            return;

        var actorCtx = ctx.AsActionContext() with { RoomId = actorRoomId };
        var permissions = await permissionService
            .ResolveForPlayerAsync(ctx.PlayerId, ct)
            .ConfigureAwait(false);

        if (!ModerationPolicy.IsAllowed(permissions, ModerationAction.Ban))
        {
            await events
                .PublishAsync(
                    new ModerationActionDeniedEvent(
                        ctx.PlayerId,
                        message.UserId,
                        actorRoomId,
                        nameof(ModerationAction.Ban)
                    ),
                    ct
                )
                .ConfigureAwait(false);
            return;
        }

        var durationSeconds = ParseBanDurationSeconds(message.BanType);
        if (durationSeconds <= 0)
            return;

        var roomGrain = grainFactory.GetRoomGrain(actorRoomId);
        await roomGrain.BanUserAsync(actorCtx, message.UserId, durationSeconds, ct).ConfigureAwait(false);
    }

    private static int ParseBanDurationSeconds(string banType)
    {
        if (string.IsNullOrWhiteSpace(banType))
            return 0;

        var normalized = banType.Trim().ToLowerInvariant();

        if (normalized is "permanent" or "perm" or "forever")
        {
            return ToBoundedSeconds(365, 86400);
        }

        var durationPartEnd = 0;

        while (durationPartEnd < normalized.Length && char.IsDigit(normalized[durationPartEnd]))
        {
            durationPartEnd++;
        }

        if (durationPartEnd == 0)
            return 0;

        if (durationPartEnd == normalized.Length && int.TryParse(normalized, out var rawMinutes) && rawMinutes > 0)
        {
            return ToBoundedSeconds(rawMinutes, 60);
        }

        if (!int.TryParse(normalized[..durationPartEnd], out var minutes) || minutes <= 0)
            return 0;

        var suffix = normalized[durationPartEnd..];
        if (string.IsNullOrWhiteSpace(suffix))
            return 0;

        return suffix switch
        {
            "m" or "min" or "mins" => ToBoundedSeconds(minutes, 60),
            "h" or "hr" or "hrs" or "hour" => ToBoundedSeconds(minutes, 3600),
            "d" or "day" => ToBoundedSeconds(minutes, 86400),
            _ => 0,
        };
    }

    private static int ToBoundedSeconds(int amount, int secondsPerUnit)
    {
        var totalSeconds = (long)amount * secondsPerUnit;

        return totalSeconds > int.MaxValue ? int.MaxValue : (int)totalSeconds;
    }
}
