namespace Vortex.Dashboard.API.Api.Catalogue.Contracts;

/// <summary>What one currency did over the whole window.</summary>
public sealed record EconomyCurrencyTotals(long Spend, long Earned, long Net, int TransactionCount);
