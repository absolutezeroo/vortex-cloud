using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Roomsettings;

[GenerateSerializer, Immutable]
public sealed record FlatControllerAddedEventMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
