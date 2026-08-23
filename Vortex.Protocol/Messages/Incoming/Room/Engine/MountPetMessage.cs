using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Room.Engine;

public record MountPetMessage : IMessageEvent
{
    public required int PetId { get; init; }

    /// <summary>The client sends the same message to get on and to get off.</summary>
    public required bool Mount { get; init; }
}
