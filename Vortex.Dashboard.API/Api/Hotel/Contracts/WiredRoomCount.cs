namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>A room and how much wired is in it. The name falls back to the id for a deleted room.</summary>
public sealed record WiredRoomCount(int RoomId, string RoomName, int WiredCount);
