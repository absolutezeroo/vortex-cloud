namespace Vortex.Dashboard.API.Api.Platform.Contracts;

/// <summary>
/// One currency balance.
/// </summary>
/// <param name="Currency">The name, falling back to the currency type and then to the id: a wallet
/// row for a currency nobody named still has to say what it is.</param>
/// <param name="ActivityPointType">Which activity point this is. Every one of them is named
/// "ActivityPoints" by <paramref name="Currency"/>, so without this the popup drew duckets and
/// diamonds as the same neutral chip -- it was asking for the field, and nothing was sending it.
/// </param>
public sealed record ProfileWallet(
    int CurrencyTypeEntityId,
    int Amount,
    string Currency,
    int? ActivityPointType
);
