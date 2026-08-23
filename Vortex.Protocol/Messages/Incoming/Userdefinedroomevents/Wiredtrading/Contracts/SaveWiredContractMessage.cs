using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms.Snapshots.Wired;

namespace Vortex.Protocol.Messages.Incoming.Userdefinedroomevents.Wiredtrading.Contracts;

/// <summary>
/// The contract editor's save button.
/// </summary>
/// <remarks>
/// It arrives in the same shape it is sent back out in, so it is read into the same snapshot — the
/// client's own editor builds the payload by handing an array to each contract subclass in turn,
/// and the reply reads it field for field.
/// </remarks>
public record SaveWiredContractMessage : IMessageEvent
{
    public required WiredContractSnapshot Contract { get; init; }
}
