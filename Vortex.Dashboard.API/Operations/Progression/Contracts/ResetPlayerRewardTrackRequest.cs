using System;
using System.Collections.Generic;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations.Progression.Contracts;

public sealed record ResetPlayerRewardTrackRequest(int PlayerId, string TrackId, string Reason)
    : IReasonedRequest;
