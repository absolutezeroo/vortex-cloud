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

    /// <summary>
    /// What the client's bot menu draws its buttons from — it reads these straight off the avatar
    /// block, so a bot serialised with none has no menu beyond being picked up.
    /// </summary>
    [Id(15)]
    public ImmutableArray<short> SkillIds { get; init; } = ImmutableArray<short>.Empty;

    /// <summary>
    /// Not part of the avatar block: like a player's, a bot's dance rides its own composer, and
    /// this is what lets a room replay it for somebody who has just walked in.
    /// </summary>
    [Id(16)]
    public AvatarDanceType DanceType { get; init; }
}
