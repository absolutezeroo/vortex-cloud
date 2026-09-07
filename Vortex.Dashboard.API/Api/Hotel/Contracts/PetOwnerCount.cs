namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>A player and how many pets they hold. The name is null for a deleted player.</summary>
public sealed record PetOwnerCount(int OwnerId, string? OwnerName, int PetCount);
