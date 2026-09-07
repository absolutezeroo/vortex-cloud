using System;
using System.Collections.Generic;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations.Progression.Contracts;

public sealed record RewardTrackRewardBody(
    int Kind,
    string RewardTypeId,
    int Amount,
    string ExtraParams,
    int SortOrder
);
