namespace Vortex.Dashboard.API.Api.Catalogue.Contracts;

/// <summary>
/// How many raffle entries ended one way.
/// </summary>
/// <param name="Result">The outcome as the raffle recorded it, which is a free string column rather
/// than an enum -- so what appears here is whatever the raffle wrote.</param>
public sealed record LtdRaffleResultCount(string Result, int Count);
