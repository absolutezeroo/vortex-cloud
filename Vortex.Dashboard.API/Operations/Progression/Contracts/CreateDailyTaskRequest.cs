using System;
using System.Collections.Generic;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

public sealed record CreateDailyTaskRequest(
    string TaskCode,
    string QuestTypeCode,
    bool IsBonus,
    string ImageVersion,
    string CatalogName,
    int RequiredRepeats,
    bool Enabled,
    int SortOrder,
    IReadOnlyList<DailyTaskRewardBody> Rewards,
    string Reason
) : IReasonedRequest;
