namespace Vortex.Dashboard.API.Api.Catalogue.Contracts;

/// <summary>One rung of the builders-club ladder: how much furniture that level may place.</summary>
public sealed record BuildersClubTierRow(int Id, int Level, int FurniLimit);
