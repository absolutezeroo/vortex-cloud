namespace Vortex.Dashboard.API.Api.Safety.Contracts;

/// <summary>Where sanctions happen. The name is null for a room that has been deleted.</summary>
public sealed record ModerationRoomCount(int RoomId, string? RoomName, int Count);
