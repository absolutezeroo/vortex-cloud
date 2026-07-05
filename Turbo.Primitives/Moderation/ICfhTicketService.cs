using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Turbo.Primitives.Moderation;

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

    Task<CfhTopicSnapshot?> GetTopicAsync(int topicId, CancellationToken ct = default);

    Task<ImmutableArray<CfhCategorySnapshot>> GetCatalogAsync(CancellationToken ct = default);
}
