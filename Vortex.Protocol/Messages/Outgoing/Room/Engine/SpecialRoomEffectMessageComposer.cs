using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Room.Engine;

[GenerateSerializer, Immutable]
public sealed record SpecialRoomEffectMessageComposer : IComposer
{
    [Id(0)]
    public required int EffectId { get; init; }
}
