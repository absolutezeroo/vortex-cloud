using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Userclassification;

[GenerateSerializer, Immutable]
public sealed record UserClassificationMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
