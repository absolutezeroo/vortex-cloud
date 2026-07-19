using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Users;

[GenerateSerializer, Immutable]
public sealed record ChangeEmailResultEventMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
