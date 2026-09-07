namespace Vortex.Dashboard.API.Api.Platform.Contracts;

/// <summary>One item the account owns, and where it currently sits.</summary>
public sealed record ProfileItemRow(
    long ItemId,
    int? DefinitionId,
    string? DefinitionName,
    string? FurniIconUrl,
    int? RoomEntityId,
    string? RoomName,
    int? RoomX,
    int? RoomY
);
