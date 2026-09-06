using System;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

/// <summary>
/// Request bodies for the content operations, each carrying a mandatory audited <c>Reason</c>.
/// Grouped in one file because they share a capability and a page-per-domain shape.
/// </summary>
public sealed record AchievementRequest(
    int AchievementId,
    string Name,
    string Category,
    int DisplayMethod,
    string Reason
) : IReasonedRequest;
