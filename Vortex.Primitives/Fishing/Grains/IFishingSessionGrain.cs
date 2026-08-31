using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Rooms;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.Primitives.Fishing.Grains;

/// <summary>
/// The fishing session one player has running, if any.
/// </summary>
/// <remarks>
/// <para>
/// <strong>A session, not a cast per fish.</strong> Origins has the player click a fish shadow once;
/// the avatar then fishes on its own, catch after catch, until the spot runs dry and they have to
/// relocate. So the client starts a session and afterwards only listens — every sighting, every
/// catch and the depletion are pushed. Modelling this as request/response per fish was the single
/// biggest error in this system's first design.
/// </para>
/// <para>
/// Keyed by player rather than by spot: the stock belongs to the spot, but the timer, the rod
/// multipliers and the reward stream belong to the person holding the rod, and one player can only
/// fish one spot at a time. Two players on the same spot draw from the same stock, which the spot
/// grain — not this one — would own; there is no such grain yet, and the ceiling is stated on
/// <see cref="StartAsync"/>.
/// </para>
/// </remarks>
public interface IFishingSessionGrain : IGrainWithIntegerKey
{
    /// <summary>
    /// Starts fishing at a spot: resolves the furni to a zone, checks the level and the daily cap,
    /// rolls the spot's stock and arms the first sighting. Pushes a
    /// <c>VortexFishingErrorMessageComposer</c> and does nothing else when the request is refused.
    /// </summary>
    /// <remarks>
    /// ponytail: the stock is rolled per session, so two players fishing the same shadow each get
    /// their own. A shared spot needs a spot-keyed grain; nothing in the client can tell the
    /// difference yet, because a sighting is only ever pushed to the session that owns it.
    /// </remarks>
    Task StartAsync(RoomId roomId, RoomObjectId spotObjectId, CancellationToken ct);

    /// <summary>
    /// Ends the session. Safe on a player who is not fishing — the client sends it on walking away,
    /// and a session also ends on its own when the spot depletes.
    /// </summary>
    Task StopAsync(CancellationToken ct);

    /// <summary>
    /// Drops the session without telling anybody: the player it belonged to is gone.
    /// </summary>
    /// <remarks>
    /// This grain is keyed by player and outlives the connection, so a disconnected player's session
    /// stays open and their next <see cref="StartAsync"/> is refused with "already fishing". The
    /// handshake calls this, which is what makes reconnecting always work. It differs from
    /// <see cref="StopAsync"/> only in sending nothing — a client that just logged in has asked for
    /// nothing and should not be told a spot it never cast at has run dry.
    /// </remarks>
    Task AbandonAsync(CancellationToken ct);

    /// <summary>
    /// The player's whole Hook Havoc attempt. Replayed here against the seed this grain issued, and
    /// answered with <c>VortexHookHavocResultMessageComposer</c>.
    /// </summary>
    Task SubmitHookHavocAsync(int[] timeline, CancellationToken ct);
}
