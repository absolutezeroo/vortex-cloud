namespace Vortex.Dashboard.API.Api.Safety.Contracts;

/// <summary>One bucket of the activity chart. Empty buckets are present with a zero.</summary>
public sealed record ModerationTimelinePoint(string Bucket, string Label, int Count);
