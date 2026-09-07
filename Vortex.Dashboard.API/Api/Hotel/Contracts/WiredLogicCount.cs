namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>One box type and how often it is used. The icon is the furniture's own.</summary>
public sealed record WiredLogicCount(string Logic, int Count, string? FurniIconUrl);
