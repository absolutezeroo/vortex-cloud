using System.Threading;
using System.Threading.Tasks;
using Orleans;

namespace Vortex.Primitives.Help.Grains;

/// <summary>
/// Who is currently on duty to answer guide, helper and guardian requests.
/// </summary>
/// <remarks>
/// Deliberately in memory and not persisted. Being on duty means "available right now", so it
/// cannot outlive the session that claimed it: a guide who closes the client while on duty must not
/// come back a day later still counted, and a server restart must start from an empty roster rather
/// than one describing a hotel that no longer exists.
/// </remarks>
public interface IGuideDirectoryGrain : IGrainWithStringKey
{
    /// <summary>
    /// Records what this player is covering, and returns the roster as they should now see it.
    /// Going off duty clears every role — the client sends its three checkboxes on every change,
    /// but an off-duty guide covers nothing whatever they are ticked as.
    /// </summary>
    Task<GuideDutySnapshot> SetDutyAsync(
        int playerId,
        bool onDuty,
        bool handlesGuideRequests,
        bool handlesHelperRequests,
        bool handlesGuardianRequests,
        CancellationToken ct
    );

    /// <summary>The roster as this player should see it, without changing anything.</summary>
    Task<GuideDutySnapshot> GetStatusAsync(int playerId, CancellationToken ct);

    /// <summary>
    /// Drops a player from the roster, for the disconnect that will otherwise leave a guide on duty
    /// who is no longer there to answer.
    /// </summary>
    Task ClearDutyAsync(int playerId, CancellationToken ct);

    /// <summary>
    /// Files a help request and offers it to one on-duty guide.
    /// </summary>
    /// <remarks>
    /// One at a time, not broadcast to everyone on duty: whoever answered first would win a race
    /// and the rest would be left dismissing a request already taken. The offer moves on when it is
    /// declined.
    /// </remarks>
    Task<GuideRequestOutcome> CreateRequestAsync(
        int requesterId,
        int helpRequestType,
        string description,
        CancellationToken ct
    );

    /// <summary>
    /// A guide's answer to the request in front of them. Accepting pairs them; declining passes it
    /// to the next guide who has not already seen it, and fails the request when there is none.
    /// </summary>
    Task<GuideRequestOutcome> GuideDecidesAsync(int guideId, bool accepted, CancellationToken ct);

    /// <summary>The session this player is in, either side of it, or null.</summary>
    Task<GuideSessionSnapshot?> GetSessionAsync(int playerId, CancellationToken ct);
}
