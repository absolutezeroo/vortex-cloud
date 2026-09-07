using System.Collections.Generic;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations.Safety.Contracts;

public sealed record CreateSanctionPresetRequest(
    int Kind,
    int PresetIndex,
    string Name,
    int? DurationSeconds,
    string? Message,
    string Reason
) : IReasonedRequest;
