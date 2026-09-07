using System;

namespace Vortex.Dashboard.API.Api.Platform.Contracts;

/// <summary>Where one item is, who owns it, and what it is carrying.</summary>
public sealed record ItemSnapshot(
    int Id,
    int? DefinitionId,
    string? DefinitionName,
    string? FurniIconUrl,
    int? OwnerPlayerId,
    string? OwnerName,
    int? RoomId,
    string? RoomName,
    int? RoomX,
    int? RoomY,
    double RoomZ,
    string? ExtraData,
    DateTime UpdatedAt
);
