using Orleans;
using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Outgoing.Room.Pets;

[GenerateSerializer, Immutable]
public sealed record PetExperienceMessageComposer : IComposer
{
    [Id(0)]
    public required int PetId { get; init; }

    [Id(1)]
    public required int Experience { get; init; }

    [Id(2)]
    public required int ExperienceForNextLevel { get; init; }

    [Id(3)]
    public required int Level { get; init; }

    [Id(4)]
    public required int MaxLevel { get; init; }
}
