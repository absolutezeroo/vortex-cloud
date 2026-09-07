using System;
using System.Collections.Generic;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations.Progression.Contracts;

/// <summary>
/// One reward. <c>RewardTypeId</c> is "credits" or an activity-point type number; anything else is
/// shown to the player but not granted, which the grain logs rather than swallowing.
/// </summary>
public sealed record DailyTaskRewardBody(
    short ProductItemTypeId,
    string RewardTypeId,
    string ExtraParams,
    int Amount
);
