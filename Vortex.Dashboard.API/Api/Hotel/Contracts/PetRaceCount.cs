namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>How many of one colour within a species.</summary>
public sealed record PetRaceCount(int Type, int Race, int Count);
