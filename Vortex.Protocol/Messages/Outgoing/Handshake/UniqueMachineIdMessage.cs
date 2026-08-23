using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Handshake;

public sealed record UniqueMachineIdMessage : IComposer
{
    public required string MachineID { get; init; }
}
