using System.Collections.Immutable;
using Orleans;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.Primitives.Rooms.Snapshots.Avatars;

[GenerateSerializer, Immutable]
public sealed record RoomBotAvatarSnapshot : RoomAvatarSnapshot
{
    [Id(12)]
    public required AvatarGenderType Gender { get; init; }

    [Id(13)]
    public required int OwnerId { get; init; }

    [Id(14)]
    public required string OwnerName { get; init; }

    [Id(15)]
    public ImmutableArray<short> SkillIds { get; init; } = ImmutableArray<short>.Empty;
}
