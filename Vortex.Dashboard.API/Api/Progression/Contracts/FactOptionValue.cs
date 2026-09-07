namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>One allowed value of a closed fact: what the engine compares, and what the operator reads.</summary>
public sealed record FactOptionValue(string Value, string LabelKey, string FallbackLabel);
