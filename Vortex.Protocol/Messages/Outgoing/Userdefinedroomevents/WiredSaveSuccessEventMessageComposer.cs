using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Userdefinedroomevents;

[GenerateSerializer, Immutable]
public sealed record WiredSaveSuccessEventMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
