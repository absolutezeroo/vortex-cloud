using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Room.Avatar;

public record AvatarExpressionMessage : IMessageEvent
{
    public required int ExpressionId { get; init; }
}
