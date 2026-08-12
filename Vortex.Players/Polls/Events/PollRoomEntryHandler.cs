using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Events.Registry;
using Vortex.Primitives.Events;
using Vortex.Primitives.Orleans;

namespace Vortex.Players.Polls.Events;

/// <summary>
/// Offers a survey when a player enters a room — the only moment the client is ready to show the
/// offer dialog, since the widget lives in the room UI. The grain decides whether anything is
/// actually eligible; most entries push nothing. The event pipeline isolates handler exceptions, so
/// a failure here never breaks room entry.
/// </summary>
public sealed class PollRoomEntryHandler(IGrainFactory grainFactory)
    : IEventHandler<PlayerEnteredRoomEvent>
{
    public async ValueTask HandleAsync(
        PlayerEnteredRoomEvent e,
        EventContext ctx,
        CancellationToken ct
    ) =>
        await grainFactory
            .GetPlayerPollGrain(e.PlayerId)
            .OfferForRoomEntryAsync(e.RoomId, ct)
            .ConfigureAwait(false);
}
