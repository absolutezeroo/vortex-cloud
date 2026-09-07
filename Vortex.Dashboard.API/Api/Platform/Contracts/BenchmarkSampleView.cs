using System;

namespace Vortex.Dashboard.API.Api.Platform.Contracts;

/// <summary>
/// One reading taken during a run.
/// </summary>
/// <param name="Failures">Sends the server refused or dropped. Non-zero invalidates the latency
/// figures above: those are the round trips that completed.</param>
public sealed record BenchmarkSampleView(
    DateTime At,
    int ConnectedClients,
    double RttMedianMs,
    double RttP95Ms,
    long PacketsReceived,
    long BytesReceived,
    long Failures
);
