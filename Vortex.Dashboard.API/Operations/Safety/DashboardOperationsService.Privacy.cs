using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Vortex.Dashboard.API.Hosting;
using Vortex.Primitives.Observability;

namespace Vortex.Dashboard.API.Operations;

/// <summary>
/// Erasure on request. Separate from the moderation operations on purpose: this is not a sanction,
/// it is the hotel answering a question a player is entitled to ask, and the operator running it is
/// doing paperwork rather than enforcement.
/// </summary>
internal sealed partial class DashboardOperationsService
{
    /// <summary>
    /// Scrubs one player's personal content out of the forensic tables. What survives, and why, is
    /// spelled out on <see cref="IForensicsPurgeService"/>.
    /// </summary>
    /// <remarks>
    /// The purge is itself audited, with the counts it produced. A privacy erasure that leaves no
    /// trace cannot be shown to have happened -- which is the one thing the person who asked for it,
    /// and anyone later asking why the record has a hole in it, both need.
    /// </remarks>
    public Task<OperationResult> PurgePlayerForensicsAsync(
        PurgePlayerForensicsRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.player.forensics_purge",
            actor,
            request.Reason,
            targetPlayerId: request.PlayerId,
            roomId: null,
            detail: new { request.PlayerId },
            work: async innerCt =>
            {
                ForensicsPurgeResult result = await _forensicsPurge
                    .PurgePlayerAsync(request.PlayerId, innerCt)
                    .ConfigureAwait(false);

                _logger.LogWarning(
                    "Operator {Actor} purged forensics for player {PlayerId}: {Chat} chat, {Visits} visits, "
                        + "{IpHashes} IP hashes, {Payloads} payloads.",
                    actor,
                    request.PlayerId,
                    result.ChatMessagesDeleted,
                    result.RoomVisitsDeleted,
                    result.AuditIpHashesCleared,
                    result.AuditPayloadsScrubbed
                );
            },
            ct,
            category: AuditCategory.Security
        );
}

/// <summary>A reason is mandatory: an erasure with no stated request behind it is indistinguishable
/// from an operator quietly destroying evidence.</summary>
internal sealed record PurgePlayerForensicsRequest(int PlayerId, string Reason) : IReasonedRequest;
