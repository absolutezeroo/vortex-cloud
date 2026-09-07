using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>What draws from a pool.</summary>
public sealed record PrizePoolBindingList(int Count, IReadOnlyList<PrizePoolBindingRow> Items);
