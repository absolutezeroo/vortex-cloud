using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.NewNavigator;

public record NavigatorRemoveCollapsedCategoryMessage : IMessageEvent
{
    public string? CategoryName { get; init; }
}
