using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Protocol.Messages.Incoming.Moderator;
using Vortex.Protocol.Messages.Outgoing.Moderation;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Orleans.Snapshots.Players;
using Vortex.Primitives.Permissions;

namespace Vortex.PacketHandlers.Moderator;

/// <summary>
/// The mod tool's user card. Carries the target's email address and sanction history, so it is
/// gated on a moderation capability rather than on anything room-scoped — the moderator looking a
/// player up is normally nowhere near them.
/// </summary>
public class GetModeratorUserInfoMessageHandler(
    IGrainFactory grainFactory,
    IPermissionService permissionService
) : IMessageHandler<GetModeratorUserInfoMessage>
{
    public async ValueTask HandleAsync(
        GetModeratorUserInfoMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || message.UserId <= 0)
        {
            return;
        }

        PermissionSet permissions = await permissionService
            .ResolveForPlayerAsync(ctx.PlayerId, ct)
            .ConfigureAwait(false);

        if (!permissions.HasAny(Capabilities.Moderation.Chatlogs, Capabilities.Room.ModerateAny))
        {
            return;
        }

        PlayerModeratorInfoSnapshot info = await grainFactory
            .GetPlayerGrain(message.UserId)
            .GetModeratorInfoAsync(ct)
            .ConfigureAwait(false);

        // Online status lives on the presence grain, not in the player's persisted row.
        bool online = await grainFactory
            .GetPlayerPresenceGrain(message.UserId)
            .IsOnlineAsync(ct)
            .ConfigureAwait(false);

        await grainFactory
            .GetPlayerPresenceGrain(ctx.PlayerId)
            .SendComposerAsync(
                new ModeratorUserInfoEventMessageComposer
                {
                    UserId = info.UserId,
                    UserName = info.UserName,
                    Figure = info.Figure,
                    RegistrationAgeInMinutes = info.RegistrationAgeInMinutes,
                    MinutesSinceLastLogin = online ? 0 : info.MinutesSinceLastLogin,
                    Online = online,
                    CfhCount = info.CfhCount,
                    AbusiveCfhCount = info.AbusiveCfhCount,
                    CautionCount = info.CautionCount,
                    BanCount = info.BanCount,
                    TradingLockCount = info.TradingLockCount,
                    TradingExpiryDate = info.TradingExpiryDate,
                    IdentityId = info.IdentityId,
                    PrimaryEmailAddress = info.PrimaryEmailAddress,
                }
            )
            .ConfigureAwait(false);
    }
}
