using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Moderator;
using Vortex.Primitives.Messages.Outgoing.Moderation;
using Vortex.Primitives.Moderation;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Permissions;

namespace Vortex.PacketHandlers.Moderator;

/// <summary>
/// A preview, not an action: the mod tool asks what the default consequence would be so it can show
/// it next to the sanction dropdown before the moderator commits. Nothing is applied here — that is
/// DefaultSanctionMessageHandler's job.
/// </summary>
public class ModToolSanctionMessageHandler(
    IGrainFactory grainFactory,
    IPermissionService permissionService,
    ISanctionPresetService sanctionPresets,
    ICfhTicketService tickets
) : IMessageHandler<ModToolSanctionMessage>
{
    public async ValueTask HandleAsync(
        ModToolSanctionMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        PermissionSet permissions = await permissionService
            .ResolveForPlayerAsync(ctx.PlayerId, ct)
            .ConfigureAwait(false);

        if (!permissions.HasAny(Capabilities.Moderation.Cfh, Capabilities.Room.ModerateAny))
        {
            return;
        }

        int topicId = await ResolveTopicIdAsync(message, ct).ConfigureAwait(false);

        CfhTopicSnapshot? topic =
            topicId > 0 ? await tickets.GetTopicAsync(topicId, ct).ConfigureAwait(false) : null;

        SanctionPresetSnapshot? preset = topic?.DefaultSanctionPresetId is int presetId
            ? await sanctionPresets.ResolveByIdAsync(presetId, ct).ConfigureAwait(false)
            : null;

        // A topic with no configured preset is a normal state, not an error — the client renders an
        // empty consequence line and the moderator picks a sanction by hand.
        await grainFactory
            .GetPlayerPresenceGrain(ctx.PlayerId)
            .SendComposerAsync(
                new SanctionInfoMessageComposer
                {
                    IssueId = message.IssueId,
                    AccountId = message.AccountId,
                    SanctionName = preset?.Name ?? topic?.Consequence ?? string.Empty,
                    SanctionLengthInHours = ToWholeHours(preset?.DurationSeconds),
                    // Every preset the CFH flow applies is an account ban (see
                    // CfhDefaultSanctionHelper), so nothing here is avatar-scoped.
                    AvatarOnly = false,
                    TradeLockInfo = string.Empty,
                    MachineBanInfo = string.Empty,
                }
            )
            .ConfigureAwait(false);
    }

    /// <summary>
    /// The client sends the topic it has selected, but sends -1 while the panel is still opening.
    /// In that case fall back to the ticket's own topic so the first render is not blank.
    /// </summary>
    private async Task<int> ResolveTopicIdAsync(
        ModToolSanctionMessage message,
        CancellationToken ct
    )
    {
        if (message.CategoryId > 0)
        {
            return message.CategoryId;
        }

        if (message.IssueId <= 0)
        {
            return 0;
        }

        CfhTicketSummary? ticket = await tickets
            .GetTicketAsync(message.IssueId, ct)
            .ConfigureAwait(false);

        return ticket?.TopicId ?? 0;
    }

    /// <summary>Null duration means a permanent sanction; the client shows 0 hours for it.</summary>
    private static int ToWholeHours(int? durationSeconds) =>
        durationSeconds is null ? 0 : durationSeconds.Value / 3600;
}
