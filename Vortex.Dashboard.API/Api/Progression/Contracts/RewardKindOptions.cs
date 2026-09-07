using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>The reward kinds a prize may hand out.</summary>
public sealed record RewardKindOptions(int Count, IReadOnlyList<RewardKindOption> Items);
