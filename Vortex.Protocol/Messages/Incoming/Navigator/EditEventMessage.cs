using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Navigator;

public record EditEventMessage : IMessageEvent
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
}
