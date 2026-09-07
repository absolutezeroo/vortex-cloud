namespace Vortex.Dashboard.API.Api.Safety.Contracts;

/// <summary>One kind of sanction a preset can be, by its stored number and its name.</summary>
public sealed record SanctionPresetKindOption(int Value, string Label);
