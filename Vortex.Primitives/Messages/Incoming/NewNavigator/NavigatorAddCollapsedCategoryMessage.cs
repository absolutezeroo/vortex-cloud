using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.NewNavigator;

public record NavigatorAddCollapsedCategoryMessage : IMessageEvent
{
    public string? CategoryName { get; init; }
}
