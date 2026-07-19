using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredmenu;

[GenerateSerializer, Immutable]
public sealed record WiredMenuErrorEventMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
