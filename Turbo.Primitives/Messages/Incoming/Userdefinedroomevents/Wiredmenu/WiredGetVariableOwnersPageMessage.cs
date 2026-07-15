using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Incoming.Userdefinedroomevents.Wiredmenu;

public record WiredGetVariableOwnersPageMessage : IMessageEvent
{
    public required string VariableId { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public required int UserTypeFilter { get; init; }
    public required int SortTypeFilter { get; init; }
}
