using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>One step of a multi-step task.</summary>
public sealed record RewardTrackStepRow(
    int StepIndex,
    string ActionCode,
    IReadOnlyList<RewardTrackFilterRow> Filters
);
