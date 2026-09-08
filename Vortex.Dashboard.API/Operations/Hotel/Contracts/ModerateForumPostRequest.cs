using Vortex.Dashboard.API.Hosting;
using Vortex.Primitives.Groups.Enums;

namespace Vortex.Dashboard.API.Operations.Hotel.Contracts;

/// <inheritdoc cref="ModerateForumThreadRequest"/>
public sealed record ModerateForumPostRequest(
    int GuildId,
    int PostId,
    ForumStaffAction Action,
    string Reason
) : IReasonedRequest;
