namespace Vortex.Dashboard.API.Api.Catalogue.Contracts;

/// <summary>
/// What a currency was actually spent on.
/// </summary>
/// <param name="Action">The audited action the debit shares a correlation id with, or
/// "uncategorized" for a debit whose audit event cannot be found -- which is a gap in the trail,
/// not a category.</param>
public sealed record EconomySpendCategory(
    string Currency,
    string Action,
    long Spend,
    int TransactionCount
);
