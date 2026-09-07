using System;
using System.Collections.Generic;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations.Progression.Contracts;

/// <summary>One action in a task's sequence, with the tests a signal must pass to satisfy it.</summary>
public sealed record RewardTrackTaskStepBody(
    string ActionCode,
    IReadOnlyList<RewardTrackStepFilterBody>? Filters
);
