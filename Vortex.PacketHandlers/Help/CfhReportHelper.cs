using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Moderation;
using Vortex.Primitives.Orleans;
using Vortex.Protocol.Messages.Outgoing.Help;

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
    /// Shown when the report names a topic this hotel has no row for. The reporter can do nothing
    /// about it — it means the client's topic list and the server's catalog disagree — but they are
    /// told, instead of watching the dialog close on a report that went nowhere.
    /// </summary>
    private const string UnknownTopicNotice =
        "Sorry, that report category is not available on this hotel. Please pick another one.";

    /// <summary>
    /// Returns false when the report was dropped — an unknown topic, or a report that names neither
    /// a person nor a room. The reporter is told either way rather than left watching a dialog close
    /// on nothing.
    /// </summary>
    /// <param name="reportedPlayerId">
    /// Who is being reported, or zero/negative when nobody is. A room report legitimately has no
    /// one: <c>CallForHelpManager.isChatSelectionRequired</c> exempts report type 4 from picking a
    /// user, so the client sends its <c>-1</c> default. Rejecting that is what made "report this
    /// room" do nothing at all.
    /// </param>
    /// <param name="roomId">The room being reported or the room the report was filed from.</param>
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
        if (reporterPlayerId <= 0 || topicId <= 0)
        {
            return false;
        }

        int? reportedPlayer = reportedPlayerId > 0 ? reportedPlayerId : null;

        // One or the other has to be there, or the ticket names nothing a moderator could act on.
        if (reportedPlayer is null && roomId is null or <= 0)
        {
            return false;
        }

        // Reporting yourself is not a moderation problem, and it would put a staff member in front
        // of a ticket whose reporter and target are the same person.
        if (reportedPlayer == reporterPlayerId)
        {
            return false;
        }

        CfhTopicSnapshot? topic = await tickets.GetTopicAsync(topicId, ct).ConfigureAwait(false);

        if (topic is null)
        {
            // The classic report form's topic ids come from the client's own window layout, not from
            // the wire, so a catalog that does not carry those ids drops every report filed through
            // it. Answering makes that visible instead of looking like a report that worked.
            await grainFactory
                .GetPlayerPresenceGrain(reporterPlayerId)
                .SendComposerAsync(
                    new CallForHelpReplyMessageComposer { Message = UnknownTopicNotice }
                )
                .ConfigureAwait(false);

            return false;
        }

        int issueId = await tickets
            .CreateTicketAsync(
                topicId,
                reporterPlayerId,
                reportedPlayer,
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
