using System;
using System.Collections.Generic;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

public sealed record RewardTrackRowRequest(int TrackRowId, string Reason) : IReasonedRequest;
