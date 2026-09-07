using System;
using System.Collections.Generic;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations.Progression.Contracts;

public sealed record UpsertRewardTrackTaskRequest(
    int TrackRowId,
    string TaskId,
    string ActionCode,
    string Parameter,
    int Mode,
    bool Premium,
    int SortOrder,
    IReadOnlyList<RewardTrackTaskLevelBody> Levels,
    IReadOnlyList<RewardTrackTaskStepBody>? Steps,
    string Reason
) : IReasonedRequest;
