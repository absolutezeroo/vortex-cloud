using Vortex.Dashboard.API.Hosting;
using Vortex.Primitives.Groups.Enums;

namespace Vortex.Dashboard.API.Operations.Hotel.Contracts;

/// <summary>Hide, restore or delete one guild forum thread.</summary>
/// <param name="GuildId">
/// Carried alongside the thread id rather than looked up from it: the forum grain is keyed by guild,
/// and a thread id that belongs to another guild has to be refused rather than moderated.
/// </param>
public sealed record ModerateForumThreadRequest(
    int GuildId,
    int ThreadId,
    ForumStaffAction Action,
    string Reason
) : IReasonedRequest;
