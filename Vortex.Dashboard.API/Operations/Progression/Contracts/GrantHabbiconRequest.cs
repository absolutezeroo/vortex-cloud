using System;
using System.Collections.Generic;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

public sealed record GrantHabbiconRequest(int PlayerId, int HabbiconId, string Reason)
    : IReasonedRequest;
