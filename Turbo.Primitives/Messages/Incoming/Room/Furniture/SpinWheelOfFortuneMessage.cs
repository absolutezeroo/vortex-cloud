using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Incoming.Room.Furniture;

public record SpinWheelOfFortuneMessage : IMessageEvent
{
    public required int ObjectId { get; init; }
}
