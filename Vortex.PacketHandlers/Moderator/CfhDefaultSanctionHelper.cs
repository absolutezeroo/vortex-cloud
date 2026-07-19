using System;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Messages.Outgoing.Moderation;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Permissions;
using Vortex.Primitives.Players;
using Vortex.Primitives.Players.Grains;

namespace Vortex.PacketHandlers.Moderator;

/// <summary>
/// Applies a CFH topic's linked <see cref="SanctionPresetSnapshot"/> as an account ban — shared by
/// CloseIssueDefaultActionMessageHandler and DefaultSanctionMessageHandler, which both ultimately
/// apply "the topic's default consequence" to a target player.
/// </summary>
internal static class CfhDefaultSanctionHelper
{
    public static async Task<bool> ApplyAsync(
        IGrainFactory grainFactory,
        ISessionGateway sessionGateway,
        ISanctionPresetService sanctionPresets,
        int sanctionPresetId,
        int targetPlayerId,
        int actorPlayerId,
        string message,
        CancellationToken ct
    )
    {
        SanctionPresetSnapshot? preset = await sanctionPresets
            .ResolveByIdAsync(sanctionPresetId, ct)
            .ConfigureAwait(false);

        if (preset is null)
        {
            return false;
        }

        DateTime bannedUntil = preset.Value.DurationSeconds is null
            ? SanctionDuration.Permanent
            : DateTime.UtcNow.AddSeconds(preset.Value.DurationSeconds.Value);

        IPlayerGrain targetGrain = grainFactory.GetPlayerGrain(targetPlayerId);
        bool success = await targetGrain
            .ApplyAccountBanAsync(actorPlayerId, bannedUntil, message, ct)
            .ConfigureAwait(false);

        if (success)
        {
            await grainFactory
                .GetPlayerPresenceGrain(targetPlayerId)
                .SendComposerAsync(new UserBannedMessageComposer { Message = message })
                .ConfigureAwait(false);
            await sessionGateway
                .RemoveSessionFromPlayerAsync(targetPlayerId, ct)
                .ConfigureAwait(false);
        }

        return success;
    }
}
