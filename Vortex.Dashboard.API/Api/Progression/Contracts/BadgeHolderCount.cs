namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>
/// One badge code and who holds it.
/// </summary>
/// <param name="Holders">Distinct players; <paramref name="Equipped"/> counts rows in a slot, so it
/// can exceed the holders when one player wears it in several.</param>
public sealed record BadgeHolderCount(
    string BadgeCode,
    string? BadgeUrl,
    int Holders,
    int Equipped
);
