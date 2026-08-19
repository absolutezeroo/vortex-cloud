using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Messages.Outgoing.Help;
using Vortex.Primitives.Moderation;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.Help;

/// <summary>
/// Shared tail of every "call for help" variant. The five entry points differ only in what the
/// client attaches — a room, an IM buffer, a photo, a forum post — but they all end the same way:
/// one CFH ticket, and one acknowledgement so the reporter knows it went somewhere.
/// </summary>
internal static class CfhReportHelper
{
    private const string SentAcknowledgement =
        "Your report has been sent to our moderation team. Thank you.";

    /// <summary>
    /// Returns false when the report was dropped — an unknown topic, or a target the server could
    /// not identify. The reporter is told either way rather than left watching a dialog close on
    /// nothing.
    /// </summary>
    public static async Task<bool> SubmitAsync(
        IGrainFactory grainFactory,
        ICfhTicketService tickets,
        int reporterPlayerId,
        int topicId,
        int reportedPlayerId,
        int? roomId,
        string message,
        IReadOnlyList<(int UserId, string Text)> evidence,
        CancellationToken ct
    )
    {
        if (reporterPlayerId <= 0 || topicId <= 0 || reportedPlayerId <= 0)
        {
            return false;
        }

        // Reporting yourself is not a moderation problem, and it would put a staff member in front
        // of a ticket whose reporter and target are the same person.
        if (reporterPlayerId == reportedPlayerId)
        {
            return false;
        }

        CfhTopicSnapshot? topic = await tickets.GetTopicAsync(topicId, ct).ConfigureAwait(false);

        if (topic is null)
        {
            return false;
        }

        int issueId = await tickets
            .CreateTicketAsync(
                topicId,
                reporterPlayerId,
                reportedPlayerId,
                roomId,
                message,
                evidence,
                ct
            )
            .ConfigureAwait(false);

        // Push it at the moderators who already have the tool open. Without this the report only
        // surfaces on their next login, which for a busy shift means never.
        await grainFactory
            .GetModerationQueueGrain()
            .PublishTicketOpenedAsync(issueId)
            .ConfigureAwait(false);

        await grainFactory
            .GetPlayerPresenceGrain(reporterPlayerId)
            .SendComposerAsync(
                new CallForHelpReplyMessageComposer { Message = SentAcknowledgement }
            )
            .ConfigureAwait(false);

        return true;
    }
}
