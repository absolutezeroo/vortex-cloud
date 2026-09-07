namespace Vortex.Dashboard.API.Api.Catalogue.Contracts;

/// <summary>
/// One currency the hotel can pay in, with what is actually held in it.
/// </summary>
/// <param name="WalletRows">How many players have a row in this currency -- which is not how many
/// hold any of it, and is the number that matters when a currency looks unused.</param>
public sealed record CurrencyTypeRow(
    int Id,
    string? Name,
    string CurrencyType,
    int? ActivityPointType,
    bool Enabled,
    int StartingAmount,
    int WalletRows,
    long TotalHeld
);
