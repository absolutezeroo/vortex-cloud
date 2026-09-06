using System;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

public sealed record GamedataEntryDeleteRequest(
    string File,
    string? Language,
    string Key,
    DateTime? ExpectedModifiedUtc,
    string Reason
) : IReasonedRequest;
