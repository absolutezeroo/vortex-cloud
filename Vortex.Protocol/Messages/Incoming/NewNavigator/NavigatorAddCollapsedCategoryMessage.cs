using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.NewNavigator;

public record NavigatorAddCollapsedCategoryMessage : IMessageEvent
{
    public string? CategoryName { get; init; }
}
