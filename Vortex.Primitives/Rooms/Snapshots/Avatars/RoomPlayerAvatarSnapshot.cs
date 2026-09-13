using Orleans;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.Primitives.Rooms.Snapshots.Avatars;

[GenerateSerializer, Immutable]
public sealed record RoomPlayerAvatarSnapshot : RoomAvatarSnapshot
{
    [Id(12)]
    public required AvatarGenderType Gender { get; init; }

    [Id(13)]
    public required AvatarDanceType DanceType { get; init; }

    [Id(14)]
    public required int GroupId { get; init; }

    [Id(15)]
    public required int GroupStatus { get; init; }

    [Id(16)]
    public required string GroupName { get; init; }

    [Id(17)]
    public required string SwimFigure { get; init; }

    [Id(18)]
    public required int ActivityPoints { get; init; }

    [Id(19)]
    public required bool IsModerator { get; init; }

    [Id(20)]
    public int CurrentEffectId { get; init; }

    /// <summary>
    /// The player's place on the hotel's badge leaderboard, or -1 for "no rank". Not 0: the client
    /// shows the row for anything <c>>= 0</c> (infostand/InfoStandUserView.as:638-646) and would
    /// print "#0" under every avatar, with a leaderboard link behind it. Nothing computes a real
    /// rank yet, so -1 is the honest answer.
    /// </summary>
    [Id(21)]
    public int BadgesRank { get; init; } = -1;

    /// <summary>What the avatar is holding; zero for empty-handed. Rides its own composer rather
    /// than the avatar block, so a room replays it for whoever has just walked in.</summary>
    [Id(22)]
    public int CarryItemId { get; init; }
}
