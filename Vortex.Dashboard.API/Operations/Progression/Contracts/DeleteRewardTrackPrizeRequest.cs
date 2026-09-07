using System;
using System.Collections.Generic;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations.Progression.Contracts;

public sealed record DeleteRewardTrackPrizeRequest(int PrizeRowId, string Reason)
    : IReasonedRequest;
