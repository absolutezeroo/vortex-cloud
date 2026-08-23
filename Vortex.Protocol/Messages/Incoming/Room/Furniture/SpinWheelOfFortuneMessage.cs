using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Room.Furniture;

public record SpinWheelOfFortuneMessage : IMessageEvent
{
    public required int ObjectId { get; init; }
}
