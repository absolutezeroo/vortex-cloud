using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.NewNavigator;

public record NavigatorRemoveCollapsedCategoryMessage : IMessageEvent
{
    public string? CategoryName { get; init; }
}
