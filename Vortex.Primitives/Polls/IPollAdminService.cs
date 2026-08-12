using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Polls.Admin;

namespace Vortex.Primitives.Polls;

/// <summary>
/// CRUD for surveys and their questions, used by the dashboard's poll admin surface. Every write
/// reloads the <see cref="Grains.IPollManagerGrain"/> cache so the surveys players are offered never
/// drift from the database — see the implementation.
/// </summary>
public interface IPollAdminService
{
    Task<PollAdminResult> CreatePollAsync(PollSpec spec, CancellationToken ct);

    Task<PollAdminResult> UpdatePollAsync(int pollId, PollSpec spec, CancellationToken ct);

    Task<PollAdminResult> DeletePollAsync(int pollId, CancellationToken ct);

    Task<PollAdminResult> CreateQuestionAsync(PollQuestionSpec spec, CancellationToken ct);

    Task<PollAdminResult> UpdateQuestionAsync(
        int questionId,
        PollQuestionSpec spec,
        CancellationToken ct
    );

    Task<PollAdminResult> DeleteQuestionAsync(int questionId, CancellationToken ct);
}
