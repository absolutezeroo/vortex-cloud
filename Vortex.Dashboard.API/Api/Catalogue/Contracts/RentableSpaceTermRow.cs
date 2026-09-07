namespace Vortex.Dashboard.API.Api.Catalogue.Contracts;

/// <summary>What renting one piece of furniture costs, and for how long.</summary>
public sealed record RentableSpaceTermRow(
    int Id,
    int FurnitureEntityId,
    int Price,
    int CurrencyTypeEntityId,
    int RentDurationSeconds,
    bool RequiresHc
);
