using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Callforhelp;

[GenerateSerializer, Immutable]
public sealed record CfhSanctionMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
