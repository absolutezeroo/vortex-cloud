using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Events.Registry;
using Vortex.Primitives.Events;
using Vortex.Primitives.Orleans;

namespace Vortex.Progression.Quests.Events;

/// <summary>
/// The one quest trigger that is not a vocabulary mapping, and so did not move onto signals.
/// </summary>
/// <remarks>
/// <para>
/// "Visit N different rooms" needs the entry's <em>own</em> timestamp:
/// <c>ProgressRoomVisitAsync</c> deduplicates against the room-entry log with
/// <c>CreatedAt &lt; enteredAtUtc</c>, which is what makes it robust to the log writer running
/// before or after it for the same entry. A <see cref="Primitives.Signals.ProgressSignal"/> carries
/// facts — things content filters on — and an entry timestamp is not one of those. Putting it on
/// the signal to serve this single call site would pollute the vocabulary the signals subsystem
/// exists to keep honest, so this stays a handler on the event itself.
/// </para>
/// <para>
/// It is therefore also the one quest trigger with no interest gate: it activates the grain on
/// every room entry. The grain early-exits on two indexed reads, which is what the original handler
/// counted on.
/// </para>
/// </remarks>
public sealed class QuestRoomVisitHandler(IGrainFactory grainFactory)
    : IEventHandler<PlayerEnteredRoomEvent>
{
    public async ValueTask HandleAsync(
        PlayerEnteredRoomEvent e,
        EventContext ctx,
        CancellationToken ct
    ) =>
        await grainFactory
            .GetPlayerQuestGrain(e.PlayerId)
            .ProgressRoomVisitAsync(e.RoomId, e.EnteredAtUtc, ct)
            .ConfigureAwait(false);
}
