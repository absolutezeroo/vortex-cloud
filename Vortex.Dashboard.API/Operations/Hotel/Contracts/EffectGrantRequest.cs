using System;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

public sealed record EffectGrantRequest(
    int PlayerId,
    int EffectId,
    int DurationSeconds,
    string Reason
) : IReasonedRequest;
