using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>What one player holds, and where each came from.</summary>
public sealed record PlayerHabbicons(
    int PlayerId,
    int Count,
    IReadOnlyList<PlayerHabbiconRow> Items
);
