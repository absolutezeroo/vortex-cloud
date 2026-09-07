using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Safety.Contracts;

/// <summary>The same events counted two ways, for the two charts that read them.</summary>
public sealed record ModerationDistribution(
    IReadOnlyList<ModerationActionCount> ByAction,
    IReadOnlyList<ModerationResultCount> ByResult
);
