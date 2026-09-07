namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>One bucket of the completion chart. Empty buckets are present with a zero.</summary>
public sealed record QuestCompletionPoint(string Bucket, string Label, int Completions);
