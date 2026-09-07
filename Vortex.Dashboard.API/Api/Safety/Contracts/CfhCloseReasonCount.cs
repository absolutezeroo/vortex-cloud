namespace Vortex.Dashboard.API.Api.Safety.Contracts;

/// <summary>How many tickets ended for one reason. The reason is the enum's own name.</summary>
public sealed record CfhCloseReasonCount(string Reason, int Count);
