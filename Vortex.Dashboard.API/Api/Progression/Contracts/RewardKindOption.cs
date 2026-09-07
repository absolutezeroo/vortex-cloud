namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>
/// One reward kind.
/// </summary>
/// <param name="Target">What the target field means for this kind, in words -- "badge code",
/// "furniture definition id". Empty for the kinds that take no target.</param>
public sealed record RewardKindOption(string Name, int Value, string Target);
