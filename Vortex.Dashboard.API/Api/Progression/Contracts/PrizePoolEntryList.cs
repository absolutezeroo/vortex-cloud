using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>Everything the pools can hand out.</summary>
public sealed record PrizePoolEntryList(int Count, IReadOnlyList<PrizePoolEntryRow> Items);
