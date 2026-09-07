namespace Vortex.Dashboard.API.Api.Platform.Contracts;

/// <summary>How many routes answer one verb.</summary>
public sealed record ApiMethodUsage(string Method, int Count);
