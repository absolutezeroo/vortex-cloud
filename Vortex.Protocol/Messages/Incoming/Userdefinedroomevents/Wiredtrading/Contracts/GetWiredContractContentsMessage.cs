using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Userdefinedroomevents.Wiredtrading.Contracts;

/// <summary>
/// "What is in this contract?"
/// </summary>
/// <remarks>
/// Sent straight back at the server after it pushes "open contract N": that push carries only an
/// id, so the round trip is not redundant.
/// </remarks>
public record GetWiredContractContentsMessage : IMessageEvent
{
    public required int ContractId { get; init; }
}
