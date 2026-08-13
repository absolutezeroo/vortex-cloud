using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Vortex.Primitives.Moderation;

public interface ICfhTicketService
{
    Task<int> CreateTicketAsync(
        int topicId,
        int reporterPlayerId,
        int reportedPlayerId,
        int? roomId,
        string message,
        IReadOnlyList<(int UserId, string Text)> evidence,
        CancellationToken ct = default
    );

    /// <summary>Ids that don't exist or aren't currently Open are silently skipped — the client
    /// sends id arrays with no server-side bundling guarantee, so partial application is expected.</summary>
    Task PickTicketsAsync(
        IReadOnlyList<int> issueIds,
        int pickerPlayerId,
        CancellationToken ct = default
    );

    Task<ImmutableArray<CfhTicketCloseOutcome>> CloseTicketsAsync(
        IReadOnlyList<int> issueIds,
        CfhTicketCloseReason reason,
        bool sanctioned,
        CancellationToken ct = default
    );

    Task ReleaseTicketsAsync(IReadOnlyList<int> issueIds, CancellationToken ct = default);

    Task<CfhTicketSummary?> GetTicketAsync(int issueId, CancellationToken ct = default);

    /// <summary>The reporter's selected evidence lines, frozen at report time — not a live
    /// re-query of the room's chatlog.</summary>
    Task<CfhTicketEvidenceSnapshot?> GetTicketEvidenceAsync(
        int issueId,
        CancellationToken ct = default
    );

    Task<CfhTopicSnapshot?> GetTopicAsync(int topicId, CancellationToken ct = default);

    Task<ImmutableArray<CfhCategorySnapshot>> GetCatalogAsync(CancellationToken ct = default);

    /// <summary>Open and Picked tickets (not Closed), most recent first — the staff mod tool's
    /// issue queue, pushed at login.</summary>
    Task<ImmutableArray<CfhIssueQueueEntrySnapshot>> GetOpenQueueAsync(
        CancellationToken ct = default
    );

    /// <summary>
    /// The reports this player filed that are still open, newest first — their own view, not the
    /// staff queue. The client asks for these before it will let them file another, so this is what
    /// stops one upset player putting the same complaint in the queue six times.
    /// </summary>
    Task<ImmutableArray<CfhPendingCallSnapshot>> GetPendingForReporterAsync(
        int reporterPlayerId,
        CancellationToken ct = default
    );

    /// <summary>
    /// Withdraws this player's own still-open reports, which is what their client offers when it
    /// shows them the pending ones. Only their own, and only ones no moderator has picked up:
    /// a report already in a moderator's hands is that moderator's to close.
    /// </summary>
    /// <returns>How many were withdrawn.</returns>
    Task<int> DeletePendingForReporterAsync(int reporterPlayerId, CancellationToken ct = default);

    /// <summary>
    /// This player's own sanction history, newest first — what their client shows under "my
    /// sanctions". Bans that have already expired are included: it is a record, not a list of what
    /// is currently in force, and a player looking at an empty screen after serving one would read
    /// it as never having been sanctioned.
    /// </summary>
    Task<ImmutableArray<PlayerSanctionSnapshot>> GetSanctionHistoryAsync(
        int playerId,
        CancellationToken ct = default
    );
}
