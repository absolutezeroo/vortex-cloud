using System;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

public sealed record DeleteAchievementLevelRequest(int LevelId, string Reason) : IReasonedRequest;
