namespace Vortex.Dashboard.API.Api.Platform.Contracts;

/// <summary>How much of each journal the window holds, before they are merged.</summary>
public sealed record RoomTimelineTotals(int Entries, int Chats, int Items);
