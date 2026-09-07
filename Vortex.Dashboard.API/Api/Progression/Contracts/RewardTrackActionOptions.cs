using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>
/// The action codes a task may be written on.
/// </summary>
/// <remarks>
/// Declared rather than derived from the translators that exist: two actions have no producer
/// today and content may already name them, so deriving would make a task written on one disappear
/// from the editor instead of being flagged inert.
/// </remarks>
public sealed record RewardTrackActionOptions(
    int Count,
    IReadOnlyList<RewardTrackActionOption> Items
);
