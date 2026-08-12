using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Orleans;

namespace Vortex.Primitives.Polls.Grains;

/// <summary>
/// Per-player survey state: which polls have been offered, declined or finished, and the answers
/// given. Every method owns its own outbound composer — callers never build one.
/// </summary>
public interface IPlayerPollGrain : IGrainWithIntegerKey
{
    /// <summary>
    /// Offers the first eligible survey for the room the player just entered, if any. Records the
    /// offer so it is never made twice, and sends the offer dialog.
    /// </summary>
    public Task OfferForRoomEntryAsync(int roomId, CancellationToken ct);

    /// <summary>
    /// The player accepted: sends the questions and marks the survey started. Sends the poll-error
    /// event instead when the survey is unknown, disabled, or already finished.
    /// </summary>
    public Task StartAsync(int pollId, CancellationToken ct);

    /// <summary>The player declined: the survey is never offered to them again.</summary>
    public Task RejectAsync(int pollId, CancellationToken ct);

    /// <summary>
    /// Records one answered question, replacing any earlier answer to it, and completes the survey
    /// once every root question has been answered.
    /// </summary>
    public Task AnswerAsync(
        int pollId,
        int questionId,
        ImmutableArray<string> answers,
        CancellationToken ct
    );
}
