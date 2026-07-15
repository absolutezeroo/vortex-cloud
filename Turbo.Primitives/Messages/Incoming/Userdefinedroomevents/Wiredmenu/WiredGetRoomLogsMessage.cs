using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Incoming.Userdefinedroomevents.Wiredmenu;

public record WiredGetRoomLogsMessage : IMessageEvent
{
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public required int LogLevelFilter { get; init; }
    public required int LogSourceFilter { get; init; }
    public required string Query { get; init; }
}
