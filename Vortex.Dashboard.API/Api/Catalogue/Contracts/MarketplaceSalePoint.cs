namespace Vortex.Dashboard.API.Api.Catalogue.Contracts;

/// <summary>
/// One bucket of the sales chart.
/// </summary>
/// <remarks>
/// Unlike the other timelines here, this one carries only the buckets that had a sale: it is built
/// from the sales themselves rather than from a pre-filled range. A quiet day is absent, not zero.
/// </remarks>
public sealed record MarketplaceSalePoint(string Bucket, string Label, int Sales, long Volume);
