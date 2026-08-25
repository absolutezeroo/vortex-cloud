using System.Threading;
using System.Threading.Tasks;

namespace Vortex.Primitives.Observability;

/// <summary>
/// Erases one player's personal content from the forensic tables on request, without erasing the
/// accounting record that other people's disputes depend on.
/// </summary>
/// <remarks>
/// <para>
/// The two obligations pull against each other. A player can ask for their personal data to be
/// erased; the hotel still has to be able to answer "where did this rare furni come from" and "who
/// was holding this ticket" months later, and those answers are about other people as much as about
/// the person asking.
/// </para>
/// <para>
/// So the split is by what the row <em>is</em>, not by whose id is on it. Free text the player
/// authored, and the technical identifiers that tie them to a machine, are destroyed. The skeleton
/// -- an action name, a timestamp, an id -- is kept, because it is the ledger, and a ledger with
/// holes in it is not a ledger. That is a defensible reading, not a legal opinion: an operator who
/// needs a harder erasure than this should say so, and it should be built deliberately.
/// </para>
/// </remarks>
public interface IForensicsPurgeService
{
    Task<ForensicsPurgeResult> PurgePlayerAsync(int playerId, CancellationToken ct = default);
}

/// <summary>What the purge actually removed, so the audit record can say it rather than imply it.</summary>
public readonly record struct ForensicsPurgeResult
{
    /// <summary>Chat lines the player wrote, deleted outright.</summary>
    public required int ChatMessagesDeleted { get; init; }

    /// <summary>Chat lines by other people that merely named them as the target; only the pointer went.</summary>
    public required int ChatMentionsCleared { get; init; }

    /// <summary>Room visits, deleted outright.</summary>
    public required int RoomVisitsDeleted { get; init; }

    /// <summary>Audit rows that had their hashed IP removed.</summary>
    public required int AuditIpHashesCleared { get; init; }

    /// <summary>Audit rows whose free-text payload was scrubbed, the row itself kept.</summary>
    public required int AuditPayloadsScrubbed { get; init; }
}
