using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Players;

namespace Vortex.Primitives.Moderation.Grains;

/// <summary>
/// The hotel's single CFH queue: who is watching it, and every transition a ticket makes while they
/// are.
/// </summary>
/// <remarks>
/// <para>
/// It exists for two reasons, both of which a plain service could not give:
/// </para>
/// <para>
/// <b>Serialization.</b> Claiming a ticket is a read-then-write across two moderators who are
/// racing on purpose — the mod tool has an auto-pick button. One grain, one turn at a time, and the
/// race is gone without a lock anywhere. This is why <see cref="PickAsync"/> lives here rather than
/// being called straight off <c>ICfhTicketService</c>.
/// </para>
/// <para>
/// <b>Addressing.</b> A ticket transition has to reach every moderator with the tool open, and the
/// alternative — walk every online player and resolve their permissions on each report — costs the
/// whole hotel to serve a handful of staff. Subscribers register once at login and are held here.
/// </para>
/// </remarks>
public interface IModerationQueueGrain : IGrainWithStringKey
{
    /// <summary>Adds a moderator who has just been sent the queue and can now receive updates to
    /// it. Idempotent: a second session for the same player does not double-deliver, because the
    /// fan-out addresses the player's presence grain rather than a session.</summary>
    Task SubscribeAsync(PlayerId moderatorId);

    /// <summary>Stops delivering queue updates to this moderator. Safe to call for a player who was
    /// never subscribed, which is the common case — every disconnect calls it.</summary>
    Task UnsubscribeAsync(PlayerId moderatorId);

    /// <summary>
    /// Remembers which room a moderator just pulled up in the room tool. The tool's caution/message
    /// buttons send no room id, so this is what tells the server where that line is meant to go.
    /// Held here rather than on the presence grain because it is moderation-session state with the
    /// same lifetime as the subscription next to it.
    /// </summary>
    Task NoteInspectedRoomAsync(PlayerId moderatorId, int roomId);

    /// <summary>The room this moderator last pulled up, or 0 if they have not opened the room tool
    /// this session.</summary>
    Task<int> GetInspectedRoomAsync(PlayerId moderatorId);

    /// <summary>Announces a newly filed report to every watching moderator.</summary>
    Task PublishTicketOpenedAsync(int issueId);

    /// <summary>
    /// Claims tickets for a moderator, then tells everyone what happened: the winner and every
    /// other watcher see the tickets move to Picked, and a caller who lost a race is sent the
    /// client's own rejection so its retry path can run.
    /// </summary>
    /// <param name="retryEnabled">Echoed back on rejection; the client uses it to decide whether to
    /// offer another attempt rather than alerting the moderator.</param>
    /// <param name="retryCount">Attempt number, echoed back so the client can stop looping.</param>
    Task PickAsync(
        PlayerId pickerId,
        IReadOnlyList<int> issueIds,
        bool retryEnabled,
        int retryCount,
        CancellationToken ct
    );

    /// <summary>Hands picked tickets back and republishes them as available.</summary>
    Task ReleaseAsync(PlayerId actorId, IReadOnlyList<int> issueIds, CancellationToken ct);

    /// <summary>
    /// Withdraws a reporter's own still-open reports and takes them off the moderators' lists. Not
    /// a moderation action — the player is retracting their own complaint — but it changes what is
    /// in the queue, so it belongs on the same path as everything else that does.
    /// </summary>
    /// <returns>How many were withdrawn.</returns>
    Task<int> WithdrawForReporterAsync(PlayerId reporterId, CancellationToken ct);

    /// <summary>
    /// Closes tickets and drops them from every watching moderator's list.
    /// </summary>
    /// <returns>
    /// One outcome per ticket actually closed, so the caller can notify the reporters. Closing is
    /// routed through here rather than done directly so that a ticket cannot be closed out from
    /// under a pick that is mid-flight.
    /// </returns>
    Task<ImmutableArray<CfhTicketCloseOutcome>> CloseAsync(
        PlayerId actorId,
        IReadOnlyList<int> issueIds,
        CfhTicketCloseReason reason,
        bool sanctioned,
        CancellationToken ct
    );
}
