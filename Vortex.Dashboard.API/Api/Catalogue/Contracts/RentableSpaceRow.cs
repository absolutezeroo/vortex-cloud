using System;

namespace Vortex.Dashboard.API.Api.Catalogue.Contracts;

/// <summary>
/// One rentable space, which is a placed piece of furniture.
/// </summary>
/// <param name="FurnitureName">The definition's name, because "#412" sends the operator to look the
/// id up somewhere else. Null when the furniture is gone.</param>
/// <param name="Rented">Rented right now: it has a renter and the rental has not run out.</param>
/// <param name="HasTerms">Whether anyone could rent it at all -- a space with no terms is placed,
/// visible, and unrentable, which is exactly the state that looks fine in the room.</param>
public sealed record RentableSpaceRow(
    int Id,
    int FurnitureId,
    string? FurnitureName,
    string? IconUrl,
    int? RenterId,
    string? RenterName,
    DateTime? RentedUntil,
    bool Rented,
    bool HasTerms
);
