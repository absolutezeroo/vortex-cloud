using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Nux;

[GenerateSerializer, Immutable]
public sealed record NewUserExperienceNotCompleteEventMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
