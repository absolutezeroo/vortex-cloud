using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Userdefinedroomevents.Wiredtrading;

/// <summary>
/// The lock row on the chest screen.
/// </summary>
/// <remarks>
/// The client sends a capacity alongside the two flags, taken from an input box on its own screen.
/// It is read so the message lines up and then ignored: capacity is bought through the upgrade, and
/// a capacity the client names is a capacity anyone can set to a million.
/// </remarks>
public record SetWiredChestLockMessage : IMessageEvent
{
    public required int ChestId { get; init; }

    public required bool Locked { get; init; }

    public required bool AutoLock { get; init; }

    public required int RequestedCapacity { get; init; }
}
