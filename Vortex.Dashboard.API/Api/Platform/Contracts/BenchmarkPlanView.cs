namespace Vortex.Dashboard.API.Api.Platform.Contracts;

/// <summary>What a run was asked to do.</summary>
public sealed record BenchmarkPlanView(
    int Players,
    int Furniture,
    int DurationSeconds,
    int RampSeconds,
    int WalkIntervalMs,
    int ChatIntervalMs,
    string Label
);
