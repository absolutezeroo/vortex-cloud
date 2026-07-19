using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Avatar;

[GenerateSerializer, Immutable]
public sealed record ChangeUserNameResultMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
