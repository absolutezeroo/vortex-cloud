using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Room.Pets;

[GenerateSerializer, Immutable]
public sealed record PetRespectFailedMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
