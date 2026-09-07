namespace Vortex.Dashboard.API.Api.Safety.Contracts;

/// <summary>One bucket of the arrival chart. Empty buckets are present with a zero.</summary>
public sealed record CfhTimelinePoint(string Bucket, string Label, int TicketsCreated);
