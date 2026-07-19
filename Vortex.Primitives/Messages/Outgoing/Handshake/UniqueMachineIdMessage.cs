using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Handshake;

public sealed record UniqueMachineIdMessage : IComposer
{
    public required string MachineID { get; init; }
}
