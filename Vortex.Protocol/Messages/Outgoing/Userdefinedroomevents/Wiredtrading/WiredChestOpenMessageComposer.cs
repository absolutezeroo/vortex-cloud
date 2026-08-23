using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Userdefinedroomevents.Wiredtrading;

/// <summary>Tells the client to open a chest's screen. The client answers by asking for its
/// contents, echoing back the same id.</summary>
[GenerateSerializer, Immutable]
public sealed record WiredChestOpenMessageComposer : IComposer
{
    [Id(0)]
    public required int ChestId { get; init; }
}
