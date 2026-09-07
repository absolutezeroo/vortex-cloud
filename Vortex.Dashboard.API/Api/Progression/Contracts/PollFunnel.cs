namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>
/// How far players got: offered to them, still pending, started, completed, refused.
/// </summary>
public sealed record PollFunnel(
    int Offered,
    int Pending,
    int Started,
    int Completed,
    int Rejected,
    double CompletionRate,
    double RejectionRate
);
