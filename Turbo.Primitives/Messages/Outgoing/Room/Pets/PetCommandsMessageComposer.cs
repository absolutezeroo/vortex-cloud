using System.Collections.Immutable;
using Orleans;
using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Outgoing.Room.Pets;

[GenerateSerializer, Immutable]
public sealed record PetCommandsMessageComposer : IComposer
{
    [Id(0)]
    public required int PetId { get; init; }

    [Id(1)]
    public ImmutableArray<int> AllCommandIds { get; init; } = [];

    [Id(2)]
    public ImmutableArray<int> EnabledCommandIds { get; init; } = [];
}
