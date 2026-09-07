namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>
/// One bucket of the growth chart. Empty buckets are present with a zero rather than skipped, so a
/// quiet week reads as a flat line instead of the chart closing the gap.
/// </summary>
public sealed record PetGrowthPoint(string Bucket, string Label, int PetsCreated);
