using System;
using System.Collections.Generic;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

public sealed record CloneRewardTrackRequest(int TrackRowId, string NewTrackId, string Reason)
    : IReasonedRequest;
