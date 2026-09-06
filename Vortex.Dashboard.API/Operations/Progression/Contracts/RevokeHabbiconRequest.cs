using System;
using System.Collections.Generic;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

public sealed record RevokeHabbiconRequest(int PlayerId, int HabbiconId, string Reason)
    : IReasonedRequest;

// Reward tracks -----------------------------------------------------------------------------
