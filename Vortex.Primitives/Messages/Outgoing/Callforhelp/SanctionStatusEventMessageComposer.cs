using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Callforhelp;

[GenerateSerializer, Immutable]
public sealed record SanctionStatusEventMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
