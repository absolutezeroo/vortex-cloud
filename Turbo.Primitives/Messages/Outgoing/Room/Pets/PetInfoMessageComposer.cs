using System.Collections.Immutable;
using Orleans;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Pets.Snapshots;

namespace Turbo.Primitives.Messages.Outgoing.Room.Pets;

[GenerateSerializer, Immutable]
public sealed record PetInfoMessageComposer : IComposer
{
    [Id(0)]
    public required PetSnapshot Pet { get; init; }

    [Id(1)]
    public required string OwnerName { get; init; }

    [Id(2)]
    public int MaxLevel { get; init; } = 20;

    [Id(3)]
    public int ExperienceRequiredToLevel { get; init; } = 100;

    [Id(4)]
    public int MaxEnergy { get; init; } = 100;

    [Id(5)]
    public int MaxNutrition { get; init; } = 100;

    [Id(6)]
    public int Age { get; init; }

    [Id(7)]
    public bool HasFreeSaddle { get; init; }

    [Id(8)]
    public bool IsRiding { get; init; }

    [Id(9)]
    public ImmutableArray<int> SkillThresholds { get; init; } = [];

    [Id(10)]
    public int AccessRights { get; init; }

    [Id(11)]
    public bool CanBreed { get; init; }

    [Id(12)]
    public bool CanHarvest { get; init; }

    [Id(13)]
    public bool CanRevive { get; init; }

    [Id(14)]
    public int RarityLevel { get; init; } = -1;

    [Id(15)]
    public int MaxWellBeingSeconds { get; init; }

    [Id(16)]
    public int RemainingWellBeingSeconds { get; init; }

    [Id(17)]
    public int RemainingGrowingSeconds { get; init; }

    [Id(18)]
    public bool HasBreedingPermission { get; init; }
}
