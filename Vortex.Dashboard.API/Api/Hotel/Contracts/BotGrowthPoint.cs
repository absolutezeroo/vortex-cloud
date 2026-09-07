namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>One bucket of the growth chart. Empty buckets are present with a zero.</summary>
public sealed record BotGrowthPoint(string Bucket, string Label, int BotsCreated);
