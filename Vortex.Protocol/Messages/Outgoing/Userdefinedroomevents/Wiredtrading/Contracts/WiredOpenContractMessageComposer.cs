using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredtrading.Contracts;

/// <summary>
/// "Open this contract's editor."
/// </summary>
/// <remarks>
/// Opening is the server's call, the same way opening a chest is: the client answers by asking for
/// the contents, and the window has no other way to appear.
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record WiredOpenContractMessageComposer : IComposer
{
    [Id(0)]
    public required int ContractId { get; init; }
}
