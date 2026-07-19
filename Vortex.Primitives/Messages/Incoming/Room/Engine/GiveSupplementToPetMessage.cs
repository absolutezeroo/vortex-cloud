using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Room.Engine;

public record GiveSupplementToPetMessage : IMessageEvent
{
    public required int PetId { get; init; }

    /// <summary>0 = water (energy), 1 = light (nutrition)</summary>
    public required int SupplementType { get; init; }
}
