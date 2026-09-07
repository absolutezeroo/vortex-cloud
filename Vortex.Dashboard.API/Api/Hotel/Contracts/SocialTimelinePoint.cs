namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>One bucket of the private-message chart. Empty buckets are present with a zero.</summary>
public sealed record SocialTimelinePoint(string Bucket, string Label, int Messages);
