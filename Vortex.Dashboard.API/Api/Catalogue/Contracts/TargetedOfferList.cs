using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Catalogue.Contracts;

/// <summary>The targeted offers, with how each has sold.</summary>
public sealed record TargetedOfferList(int Count, IReadOnlyList<TargetedOfferRow> Items);
