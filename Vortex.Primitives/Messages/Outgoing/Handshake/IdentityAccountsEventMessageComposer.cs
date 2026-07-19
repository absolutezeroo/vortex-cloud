using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Handshake;

[GenerateSerializer, Immutable]
public sealed record IdentityAccountsEventMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
