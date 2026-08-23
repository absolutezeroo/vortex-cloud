using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Userdefinedroomevents.Wiredtrading;

/// <summary>
/// Locking or unlocking a room's chests from the wired menu.
/// </summary>
/// <remarks>
/// The message names no chest, so the scope is the room. <see cref="ApplyToAll"/> is the flag the
/// confirmed lock-them-all button sets and the plain lock button does not; what the official server
/// does differently between the two is not visible from the client, so both are applied the same way
/// here rather than inventing a distinction.
/// </remarks>
public record SetAllWiredChestLocksMessage : IMessageEvent
{
    public required bool Locked { get; init; }

    public required bool ApplyToAll { get; init; }
}
