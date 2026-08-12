using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Orleans;

namespace Vortex.Primitives.Players.Grains;

/// <summary>
/// The help quizzes for one player. Owns the answer key — grading never leaves the server — and
/// sends its own composers.
/// </summary>
public interface IPlayerQuizGrain : IGrainWithIntegerKey
{
    /// <summary>Sends the question list for a quiz, or nothing at all when the code is unknown or
    /// disabled: the client has no error screen for a quiz, so an empty window is worse than none.</summary>
    public Task RequestAsync(string quizCode, CancellationToken ct);

    /// <summary>Marks a submission, grants the reward badge on a first pass, and sends the result.</summary>
    public Task SubmitAsync(string quizCode, ImmutableArray<int> answers, CancellationToken ct);
}
