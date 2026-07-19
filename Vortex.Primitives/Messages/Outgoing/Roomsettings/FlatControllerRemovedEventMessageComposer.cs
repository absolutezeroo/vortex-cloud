using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Roomsettings;

[GenerateSerializer, Immutable]
public sealed record FlatControllerRemovedEventMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
