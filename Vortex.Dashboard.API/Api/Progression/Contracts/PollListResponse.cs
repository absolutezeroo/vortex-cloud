using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>Every survey an operator may edit, with the counts that say whether it is working.</summary>
public sealed record PollListResponse(int Count, IReadOnlyList<PollListItem> Items);
