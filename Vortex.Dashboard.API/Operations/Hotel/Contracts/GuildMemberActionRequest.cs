using Vortex.Dashboard.API.Hosting;
using Vortex.Primitives.Groups.Enums;

namespace Vortex.Dashboard.API.Operations.Hotel.Contracts;

/// <summary>Answer a membership request, remove a member, or lift a guild ban.</summary>
/// <remarks>
/// One request record for five actions because they are one decision about one player in one guild,
/// and the audit row reads better for it: the action is a field of the operation rather than five
/// endpoints that differ by their URL.
/// </remarks>
public sealed record GuildMemberActionRequest(
    int GuildId,
    int PlayerId,
    GroupStaffAction Action,
    string Reason
) : IReasonedRequest;
