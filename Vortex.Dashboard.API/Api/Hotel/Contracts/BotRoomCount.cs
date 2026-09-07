namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>Which rooms hold the most bots. The name is null for a room that has been deleted.</summary>
public sealed record BotRoomCount(int RoomId, string? RoomName, int BotCount);
