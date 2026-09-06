using System;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

public sealed record DeleteAchievementRequest(int AchievementId, string Reason) : IReasonedRequest;
