using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms.Snapshots.Wired;

namespace Vortex.Protocol.Messages.Outgoing.Userdefinedroomevents.Wiredtrading.Contracts;

/// <summary>Everything about one wired contract.</summary>
[GenerateSerializer, Immutable]
public sealed record WiredContractContentsMessageComposer : IComposer
{
    [Id(0)]
    public required WiredContractSnapshot Contract { get; init; }
}
