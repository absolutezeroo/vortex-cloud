using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Roomsettings;

[GenerateSerializer, Immutable]
public sealed record NoSuchFlatEventMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
