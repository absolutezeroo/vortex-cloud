using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents;

[GenerateSerializer, Immutable]
public sealed record WiredRewardResultMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
