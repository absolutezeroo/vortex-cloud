using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Catalogue.Contracts;

/// <summary>
/// What the two box pools can hand out.
/// </summary>
/// <remarks>
/// Only the box and trophy pools: a seasonal crackable pool showing up in this table would read as
/// a box prize that never drops.
/// </remarks>
public sealed record MysteryBoxPrizeList(int Count, IReadOnlyList<MysteryBoxPrizeRow> Items);
