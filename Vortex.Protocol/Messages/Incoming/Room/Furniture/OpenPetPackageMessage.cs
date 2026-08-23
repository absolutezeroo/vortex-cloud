using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Room.Furniture;

public record OpenPetPackageMessage : IMessageEvent
{
    public required int ObjectId { get; init; }

    /// <summary>The name the player typed for the pet inside.</summary>
    public required string Name { get; init; }
}
