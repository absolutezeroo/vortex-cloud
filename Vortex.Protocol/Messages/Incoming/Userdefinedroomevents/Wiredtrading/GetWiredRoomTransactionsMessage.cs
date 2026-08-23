using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Userdefinedroomevents.Wiredtrading;

/// <summary>
/// Every chest in the room, a page at a time.
/// </summary>
/// <remarks>
/// Sent by the wired menu's chests tab, which asks twice: once with a small page for its preview,
/// and again with a full page when someone opens the log itself.
/// </remarks>
public record GetWiredRoomTransactionsMessage : IMessageEvent
{
    public required int PageSize { get; init; }

    public required int Page { get; init; }
}
