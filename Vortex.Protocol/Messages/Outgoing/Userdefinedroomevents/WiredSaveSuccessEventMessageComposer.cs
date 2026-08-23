using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents;

[GenerateSerializer, Immutable]
public sealed record WiredSaveSuccessEventMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
